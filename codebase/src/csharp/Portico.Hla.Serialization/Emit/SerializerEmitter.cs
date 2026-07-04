using System;
using System.Reflection;
using System.Reflection.Emit;
using Portico.Hla.Serialization.Io;
using Portico.Hla.Serialization.Metadata;

namespace Portico.Hla.Serialization.Emit
{
    /// <summary>
    /// Generates encode/decode delegates for a type by emitting IL into DynamicMethods
    /// (WCF DataContractSerializer-style). Emitted once per type on first use, then cached and
    /// invoked with no further reflection.
    ///
    /// Nested records are NOT inlined: emitted IL calls back into
    /// <see cref="SerializerRuntime.SerializeRecordInto"/> / <see cref="SerializerRuntime.DeserializeRecord"/>,
    /// which dispatch by runtime type. This keeps codegen simple and safe for recursive/cyclic graphs.
    /// </summary>
    internal static class SerializerEmitter
    {
        private static readonly MethodInfo SerializeRecordInto =
            typeof(SerializerRuntime).GetMethod(nameof(SerializerRuntime.SerializeRecordInto),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo DeserializeRecord =
            typeof(SerializerRuntime).GetMethod(nameof(SerializerRuntime.DeserializeRecord),
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Type), typeof(HlaReader) }, null);

        private static readonly MethodInfo WriteArrayMethod =
            typeof(SerializerRuntime).GetMethod(nameof(SerializerRuntime.WriteArray),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo ReadArrayMethod =
            typeof(SerializerRuntime).GetMethod(nameof(SerializerRuntime.ReadArray),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo GetTypeFromHandle =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle),
                BindingFlags.Public | BindingFlags.Static);

        private const BindingFlags CtorFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // ---- record whole-value delegates ---------------------------------------------------

        /// <summary>Emit <c>void (object record, HlaWriter writer)</c> writing all fields in order.</summary>
        public static Action<object, HlaWriter> EmitRecordSerializer(TypeDescriptor descriptor)
        {
            var dm = new DynamicMethod(
                "HlaRecSer_" + descriptor.ClrType.Name,
                typeof(void),
                new[] { typeof(object), typeof(HlaWriter) },
                typeof(SerializerRuntime).Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();
            foreach (MemberBinding member in descriptor.Members)
                EmitWriteMember(il, descriptor.ClrType, member, objArg: 0, writerArg: 1);
            il.Emit(OpCodes.Ret);

            return (Action<object, HlaWriter>)dm.CreateDelegate(typeof(Action<object, HlaWriter>));
        }

        /// <summary>Emit <c>object (HlaReader reader)</c> constructing the instance and reading all fields.</summary>
        public static Func<HlaReader, object> EmitRecordDeserializer(TypeDescriptor descriptor)
        {
            var dm = new DynamicMethod(
                "HlaRecDes_" + descriptor.ClrType.Name,
                typeof(object),
                new[] { typeof(HlaReader) },
                typeof(SerializerRuntime).Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();
            LocalBuilder instance = il.DeclareLocal(descriptor.ClrType);

            ConstructorInfo ctor = descriptor.ClrType.GetConstructor(CtorFlags, null, Type.EmptyTypes, null);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, instance);

            foreach (MemberBinding member in descriptor.Members)
                EmitReadMember(il, member, () => il.Emit(OpCodes.Ldloc, instance), readerArg: 0);

            il.Emit(OpCodes.Ldloc, instance);
            il.Emit(OpCodes.Ret);

            return (Func<HlaReader, object>)dm.CreateDelegate(typeof(Func<HlaReader, object>));
        }

        // ---- per-member delegates (object attributes / interaction parameters) --------------

        /// <summary>Emit <c>void (object owner, HlaWriter writer)</c> writing a single member's value.</summary>
        public static Action<object, HlaWriter> EmitMemberEncoder(Type ownerType, MemberBinding member)
        {
            var dm = new DynamicMethod(
                "HlaMemSer_" + ownerType.Name + "_" + member.Property.Name,
                typeof(void),
                new[] { typeof(object), typeof(HlaWriter) },
                typeof(SerializerRuntime).Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();
            EmitWriteMember(il, ownerType, member, objArg: 0, writerArg: 1);
            il.Emit(OpCodes.Ret);

            return (Action<object, HlaWriter>)dm.CreateDelegate(typeof(Action<object, HlaWriter>));
        }

        /// <summary>Emit <c>void (object owner, HlaReader reader)</c> reading and assigning one member.</summary>
        public static Action<object, HlaReader> EmitMemberDecoder(Type ownerType, MemberBinding member)
        {
            var dm = new DynamicMethod(
                "HlaMemDes_" + ownerType.Name + "_" + member.Property.Name,
                typeof(void),
                new[] { typeof(object), typeof(HlaReader) },
                typeof(SerializerRuntime).Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();
            EmitReadMember(il, member,
                loadOwner: () =>
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, ownerType);
                },
                readerArg: 1);
            il.Emit(OpCodes.Ret);

            return (Action<object, HlaReader>)dm.CreateDelegate(typeof(Action<object, HlaReader>));
        }

        // ---- shared building blocks ---------------------------------------------------------

        private static void EmitWriteMember(ILGenerator il, Type ownerType, MemberBinding member,
            int objArg, int writerArg)
        {
            MethodInfo getter = member.Property.GetGetMethod(true);

            if (member.IsArray)
            {
                // SerializerRuntime.WriteArray( ((Owner)obj).Getter, writer, typeof(Elem), elemDataType )
                EmitLdarg(il, objArg);
                il.Emit(OpCodes.Castclass, ownerType);
                il.Emit(OpCodes.Callvirt, getter);
                EmitLdarg(il, writerArg);
                il.Emit(OpCodes.Ldtoken, member.ElementClrType);
                il.Emit(OpCodes.Call, GetTypeFromHandle);
                EmitDataTypeString(il, member.ElementDataType);
                il.Emit(OpCodes.Call, WriteArrayMethod);
            }
            else if (member.IsPrimitive)
            {
                // writer.WriteXxx( ((Owner)obj).Getter )
                EmitLdarg(il, writerArg);
                EmitLdarg(il, objArg);
                il.Emit(OpCodes.Castclass, ownerType);
                il.Emit(OpCodes.Callvirt, getter);
                il.Emit(OpCodes.Call, member.Primitive.WriteMethod);
            }
            else
            {
                // SerializerRuntime.SerializeRecordInto( ((Owner)obj).Getter, writer )
                EmitLdarg(il, objArg);
                il.Emit(OpCodes.Castclass, ownerType);
                il.Emit(OpCodes.Callvirt, getter);
                EmitLdarg(il, writerArg);
                il.Emit(OpCodes.Call, SerializeRecordInto);
            }
        }

        private static void EmitReadMember(ILGenerator il, MemberBinding member,
            Action loadOwner, int readerArg)
        {
            MethodInfo setter = member.Property.GetSetMethod(true);

            if (member.IsArray)
            {
                // owner.SetXxx( (PropType) SerializerRuntime.ReadArray(reader, typeof(Elem), elemDataType, isList) )
                loadOwner();
                EmitLdarg(il, readerArg);
                il.Emit(OpCodes.Ldtoken, member.ElementClrType);
                il.Emit(OpCodes.Call, GetTypeFromHandle);
                EmitDataTypeString(il, member.ElementDataType);
                il.Emit(member.IsList ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Call, ReadArrayMethod);
                il.Emit(OpCodes.Castclass, member.Property.PropertyType);
                il.Emit(OpCodes.Callvirt, setter);
            }
            else if (member.IsPrimitive)
            {
                // owner.SetXxx( reader.ReadXxx() )
                loadOwner();
                EmitLdarg(il, readerArg);
                il.Emit(OpCodes.Call, member.Primitive.ReadMethod);
                il.Emit(OpCodes.Callvirt, setter);
            }
            else
            {
                // owner.SetXxx( (MemberType) SerializerRuntime.DeserializeRecord(typeof(MemberType), reader) )
                loadOwner();
                il.Emit(OpCodes.Ldtoken, member.RecordType);
                il.Emit(OpCodes.Call, GetTypeFromHandle);
                EmitLdarg(il, readerArg);
                il.Emit(OpCodes.Call, DeserializeRecord);
                il.Emit(OpCodes.Castclass, member.RecordType);
                il.Emit(OpCodes.Callvirt, setter);
            }
        }

        private static void EmitDataTypeString(ILGenerator il, string dataType)
        {
            if (dataType == null)
                il.Emit(OpCodes.Ldnull);
            else
                il.Emit(OpCodes.Ldstr, dataType);
        }

        private static void EmitLdarg(ILGenerator il, int index)
        {
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldarg_0); break;
                case 1: il.Emit(OpCodes.Ldarg_1); break;
                case 2: il.Emit(OpCodes.Ldarg_2); break;
                case 3: il.Emit(OpCodes.Ldarg_3); break;
                default: il.Emit(OpCodes.Ldarg_S, (byte)index); break;
            }
        }
    }
}
