// (c) 2023 Jamie Clarkson
// This code is licensed under MIT license (see LICENSE for details)

namespace Jamn.NET;

/// <summary>
/// Class <c>SerializableAttribute</c> can be applied to classes and fields/properties.
/// </summary>
public class SerializableAttribute : Attribute
{
    public bool UniqueObject { get; set; }

}

/// <summary>
/// Interface <c>IValue</c> represents a Jamn object.
/// </summary>
public interface IValue
{
    public string PseudoType { get; init; }

}

/// <summary>
/// Class <c>Value</c> represents a basic value.
/// </summary>
public class Value : IValue
{
    public string PseudoType { get; init; }
    public object Object { get; init; }

    public Value(object o, string ptype = "")
    {
        PseudoType = ptype;
        Object = o;
    }
}

/// <summary>
/// Class <c>Object</c> represents a Jamn object/dict.
/// </summary>
public class Object : IValue
{
    public Dictionary<string, object> Fields { get; init; }
    public string PseudoType { get; init; }
    public Object(string ptype = "")
    {
        Fields = new Dictionary<string, object>();
        PseudoType = ptype;
    }
}

/// <summary>
/// Class <c>Array</c> represents a Jamn array.
/// </summary>
public class Array : IValue
{
    public List<object> Elems { get; init; }
    public string PseudoType { get; init; }

    public Array(string ptype = "")
    {
        Elems = new List<object>();
        PseudoType = ptype;
    }
}