// (c) 2023 Jamie Clarkson
// This code is licensed under MIT license (see LICENSE for details)

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jamn.NET;

/// <summary>
/// Class <c>Serializer</c> is used to convert an object graph into a Jamn document or vise-versa.
/// </summary>
public partial class Serializer
{
    /// <summary>
    /// <c>Serialize</c> writes an object graph into a stream.
    /// </summary>
    public static void Serialize(Stream stream, System.Object obj)
    {
        // Top level

        using (var writer = new StreamWriter(stream))
        {
            var e = new Encoder(writer, obj);

            e.WriteTopLevel();
        }
    }

    class Encoder
    {

        StreamWriter writer;

        System.Object objectGraph;

        public Encoder(StreamWriter w, System.Object objGraph)
        {
            writer = w;
            objectGraph = objGraph;
        }
        void WriteValue(System.Object obj, bool endNewline = true)
        {
            if (obj is Object jObj)
            {
                if (jObj.PseudoType != "")
                {
                    //TODO: Check if is valid naked
                    writer.WriteLine(@"${0} {{", jObj.PseudoType);

                    foreach (var f in jObj.Fields)
                    {
                        //TODO: Check if key is valid naked
                        writer.Write(f.Key + ": ");
                        WriteValue(f.Value);


                    }
                    writer.Write("}");
                }
                else
                {
                    writer.WriteLine("{");

                    foreach (var f in jObj.Fields)
                    {
                        writer.Write(f.Key + ": ");
                        WriteValue(f.Value);


                    }


                    writer.Write("}");
                }
            }
            else if (obj is Array jArray)
            {
                if (jArray.PseudoType != "")
                {
                    //TODO: Check if is valid naked
                    writer.WriteLine(@"${0} [", jArray.PseudoType);
                    foreach (var elem in jArray.Elems)
                    {
                        WriteValue(elem);
                    }
                    writer.Write("]");
                }
                else
                {
                    writer.WriteLine("[");
                    foreach (var elem in jArray.Elems)
                    {
                        WriteValue(elem);
                    }
                    writer.Write("]");
                }
            }
            else if (obj == null)
            {
                writer.Write("%null");

            }
            else if (obj is String s)
            {
                writer.Write(@"""{0}""", s);
            }
            else if (obj is Byte b)
            {
                writer.Write("$u8 {0}", b);
            }
            else if (obj is Int32 i32)
            {
                writer.Write("$i32 {0}", i32);
            }
            else if (obj is Int64 i64)
            {
                writer.Write("$i64 {0}", i64);
            }
            else if (obj is Single f32)
            {
                writer.Write("$f32 {0}", f32);
            }
            else if (obj is Double f64)
            {
                writer.Write("$f64 {0}", f64);
            }
            else
            {
                var targetType = obj.GetType();
                if (targetType.IsDefined(typeof(SerializableAttribute), true))
                {

                    var attrib = Attribute.GetCustomAttribute(targetType, typeof(SerializableAttribute));

                    if (attrib is SerializableAttribute sattrib)
                    {
                        if (sattrib.UniqueObject)
                        {
                            bool known;
                            var id = RuntimeHelpers.GetHashCode(obj);

                            // hash may be shared, use ReferenceEquals to determine if correct
                        }
                    }
                    //Author author in property.GetCustomAttributes(typeof(Author), true).Cast<Author>()
                    //var attr = targetType.GetCustomAttributes(typeof(SerializableAttribute));

                    IEnumerable<MemberInfo> serializableMembers =
                        targetType.GetMembers(BindingFlags.GetField |
                        BindingFlags.GetProperty |
                        BindingFlags.Instance | BindingFlags.Public);

                    writer.WriteLine("$" + targetType.FullName + " {");

                    foreach (MemberInfo memberInfo in serializableMembers)
                    {
                        var methodInfo = memberInfo as MethodInfo;

                        if (methodInfo != null) { continue; }

                        var ctorInfo = memberInfo as ConstructorInfo;

                        if (ctorInfo != null) { continue; }

                        writer.Write(memberInfo.Name + ": ");

                        if (memberInfo is FieldInfo fieldInfo)
                        {
                            WriteValue(fieldInfo!.GetValue(obj));
                        }
                        else if (memberInfo is PropertyInfo propInfo)
                        {
                            WriteValue(propInfo!.GetValue(obj));
                        }
                    }

                    writer.Write("}");
                }
                else if (targetType.IsArray)
                {
                    var elemType = targetType.GetElementType();

                    var arr = (System.Array)obj;

                    var name = GetDefaultTypeForSystem(elemType);

                    if (name == "")
                    {
                        name = elemType.FullName;
                    }

                    writer.WriteLine(@"${0}_{1} [", name, arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var elem = arr.GetValue(i);
                        WriteValue(elem!);
                    }

                    writer.Write("]");
                }
                else
                {
                    writer.Write("%error");
                }
            }

            if (endNewline) { writer.Write("\n"); }
            else { writer.Write(";"); }
        }

        public void WriteTopLevel()
        {
            if (objectGraph is Object jObj)
            {
                if (jObj.PseudoType != "")
                {
                    WriteValue(objectGraph);
                }
                else
                {
                    foreach (var f in jObj.Fields)
                    {
                        writer.Write(f.Key + ": ");
                        WriteValue(f.Value);

                    }
                }
            }
            else if (objectGraph is Array jArray)
            {
                if (jArray.PseudoType != "")
                {
                    WriteValue(objectGraph);
                }
                else
                {
                    foreach (var elem in jArray.Elems)
                    {

                        WriteValue(elem);

                    }
                }
            }
            else
            {
                WriteValue(objectGraph);
            }
        }

        string GetDefaultTypeForSystem(Type t)
        {
            if (t == typeof(String))
            {
                return "str";
            }
            else if (t == typeof(Byte))
            {
                return "u8";
            }
            else if (t == typeof(Int16))
            {
                return "i16";
            }
            else if (t == typeof(Int32))
            {
                return "i32";
            }
            else if (t == typeof(Int64))
            {
                return "i64";
            }
            else if (t == typeof(UInt16))
            {
                return "u16";
            }
            else if (t == typeof(UInt32))
            {
                return "u32";
            }
            else if (t == typeof(UInt64))
            {
                return "u64";
            }
            else if (t == typeof(Single))
            {
                return "f32";
            }
            else if (t == typeof(Double))
            {
                return "f64";
            }

            return "";
        }

        bool IsValidNakedString(string s)
        {
            return true;
        }
    }


}
