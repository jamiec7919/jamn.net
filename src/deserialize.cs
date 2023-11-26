// (c) 2023 Jamie Clarkson
// This code is licensed under MIT license (see LICENSE for details)

using System.Text;

namespace Jamn.NET;

/// <summary>
/// Class <c>Serializer</c> is used to convert an object graph into a Jamn document or vise-versa.
/// </summary>
public partial class Serializer
{
    static Serializer defaultSerializer = new Serializer();

    /// <summary>
    /// <c>Deserialize</c> parses a stream and returns an object graph.
    /// </summary>
    public static object Deserialize(Stream stream)
    {
        // Top level

        using (var reader = new StreamReader(stream))
        {
            var d = new Decoder(reader);

            return d.ParseTopLevel();
        }

    }


    class Decoder
    {
        enum TokenType
        {
            String,
            Number,
            PseudoType,
            SpecialValue,
            Encoding,
            Terminator,
            Colon,
            LBrace,
            RBrace,
            LSq,
            RSq,
            EOF
        }

        struct Token
        {
            public TokenType Type;
            public string Value;
        }

        StreamReader reader;
        int peek;

        public Decoder(StreamReader _reader)
        {
            reader = _reader;
            peek = -1;
        }

        void ParseObject(Object obj, bool topLevel = false, string firstLabel = "")
        {

            if (topLevel && firstLabel != "")
            {
                var v = ParseValue();

                obj.Fields[firstLabel] = v;

            }

            while (true)
            {
                var tok = NextToken();

                if (tok.Type == TokenType.RBrace)
                {
                    if (NextToken(true).Type != TokenType.Terminator)
                    {
                        throw new Exception("Object not terminated");
                    }
                    return;
                }

                if (topLevel && tok.Type == TokenType.EOF)
                {
                    return;
                }

                if (tok.Type != TokenType.String)
                {
                    throw new Exception("Invalid token for label");
                }

                if (NextToken().Type != TokenType.Colon)
                {
                    throw new Exception("Expected : for label");
                }

                var v = ParseValue();

                obj.Fields[tok.Value] = v;
            }
        }

        void ParseArray(Array arr)
        {

            while (true)
            {
                var v = ParseArrayValue();

                if (v == null)
                {
                    break;
                }

                arr.Elems.Add(v);

            }

            if (NextToken(true).Type != TokenType.Terminator)
            {
                throw new Exception("Object not terminated");
            }
        }

        class Label
        {
            public string Value = "";
        }


        object ParseArrayValue(bool topLevel = false)
        {
            var tok = NextToken();

            if (!topLevel && tok.Type == TokenType.RSq)
            {
                return null;
            }

            string pseudotype = "";
            object? obj = null;

            if (tok.Type == TokenType.PseudoType)
            {
                pseudotype = tok.Value;

                tok = NextToken();
            }

            if (tok.Type == TokenType.LBrace)
            {
                var o = new Object(pseudotype);
                obj = o;
                ParseObject(o);
                return obj;
            }
            else if (tok.Type == TokenType.LSq)
            {
                var arr = new Array(pseudotype);
                obj = arr;
                ParseArray(arr);
                return obj;
            }
            else if (tok.Type == TokenType.String)
            {
                var next = NextToken(true);

                if (topLevel && next.Type == TokenType.Colon)
                {
                    var o = new Object(pseudotype);
                    obj = o;

                    ParseObject(o, true, tok.Value);
                    return obj;
                }

                if (next.Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }

                obj = tok.Value;
            }
            else if (tok.Type == TokenType.Number)
            {
                if (NextToken(true).Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }
                obj = Convert.ChangeType(tok.Value, typeof(Double));
            }
            else if (tok.Type == TokenType.SpecialValue)
            {
                if (NextToken(true).Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }
                obj = tok.Value;

            }
            else if (tok.Type == TokenType.EOF)
            {
                if (topLevel)
                {
                    return null;
                }

                throw new Exception("EOF detected in array");
            }
            else
            {
                throw new Exception("Token not allowed at this position");
            }

            if (pseudotype != "")
            {
                return new Value(obj!, pseudotype);
            }

            return obj!;

        }

        object ParseValue()
        {
            var tok = NextToken();

            string pseudotype = "";
            object? obj = null;

            if (tok.Type == TokenType.PseudoType)
            {
                pseudotype = tok.Value;
                tok = NextToken();
            }

            if (tok.Type == TokenType.LBrace)
            {
                var o = new Object(pseudotype);
                obj = o;
                ParseObject(o);
                return obj;
            }
            else if (tok.Type == TokenType.LSq)
            {
                var arr = new Array(pseudotype);
                obj = arr;
                ParseArray(arr);
                return obj;
            }
            else if (tok.Type == TokenType.String)
            {
                if (NextToken(true).Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }
                obj = tok.Value;
            }
            else if (tok.Type == TokenType.Number)
            {
                if (NextToken(true).Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }

                if (pseudotype == "f32")
                {
                    obj = Convert.ChangeType(tok.Value, typeof(Single));

                }
                else if (pseudotype == "i32")
                {
                    obj = Convert.ChangeType(tok.Value, typeof(Int32));

                }
                else
                {
                    obj = Convert.ChangeType(tok.Value, typeof(Double));
                }
            }
            else if (tok.Type == TokenType.SpecialValue)
            {
                if (NextToken(true).Type != TokenType.Terminator)
                {
                    throw new Exception("Object not terminated");
                }
                obj = tok.Value;

            }
            else if (tok.Type == TokenType.EOF)
            {
                return null;
            }
            else
            {
                throw new Exception("Not allowed token at this position");
            }

            if (pseudotype != "")
            {
                return new Value(obj!, pseudotype);
            }

            return obj!;

        }

        public object ParseTopLevel()
        {
            var a = new Array();

            while (true)
            {
                var v = ParseArrayValue(true);

                if (v == null)
                {
                    break;
                }

                a.Elems.Add(v);

            }

            // If only one object then don't wrap
            if (a.Elems.Count == 1)
            {
                return a.Elems[0];
            }

            return a;

        }

        public bool IsLegalNakedChar(Char ch, bool starting)
        {
            if (Char.IsLetter(ch) || ch == '_' || ch == '.')
            {

                return true;
            }

            if (!starting && Char.IsNumber(ch))
            {
                return true;
            }

            return false;
        }


        Token NextToken(bool insertTerminator = false)
        {

            while (true)
            {
                var c = Peek();

                if (c == -1)
                {
                    if (insertTerminator)
                    {
                        return new Token { Type = TokenType.Terminator };
                    }

                    return new Token { Type = TokenType.EOF };
                };

                var ch = (Char)c;

                //Console.WriteLine("C.. {0}", ch);

                if (IsLegalNakedChar(ch, true) || ch == '"')
                {
                    // Console.WriteLine("Str.. {0}", ch);

                    return ParseString();
                }


                if (Char.IsDigit(ch) || ch == '-')
                {
                    return ParseNumber();
                }

                Next();

                if (insertTerminator && ch == '\n')
                {
                    return new Token { Type = TokenType.Terminator };

                }

                if (Char.IsWhiteSpace(ch))
                {
                    continue;
                }

                if (ch == '$')
                {
                    var str = ParseString();

                    if (str.Type != TokenType.String)
                    {
                        throw new Exception("PseudoType not string value");
                    }

                    if (str.Value.Length < 1)
                    {
                        throw new Exception("PseudoType must not be empty string");
                    }


                    return new Token { Type = TokenType.PseudoType, Value = str.Value };
                }

                if (ch == '%')
                {
                    var str = ParseString();

                    if (str.Type != TokenType.String)
                    {
                        throw new Exception("SpecialValue not string value");
                    }

                    if (str.Value.Length < 1)
                    {
                        throw new Exception("SpecialValue must not be empty string");
                    }

                    return new Token { Type = TokenType.SpecialValue, Value = str.Value };
                }

                if (ch == '=')
                {
                    var str = ParseString();

                    if (str.Type != TokenType.String)
                    {
                        throw new Exception("Encoding not string value");
                    }

                    if (str.Value.Length < 1)
                    {
                        throw new Exception("Encoding must not be empty string");
                    }

                    return new Token { Type = TokenType.Encoding, Value = str.Value };
                }

                if (ch == ';')
                {
                    return new Token { Type = TokenType.Terminator };
                }

                if (ch == ':')
                {
                    return new Token { Type = TokenType.Colon };
                }
                if (ch == '{')
                {
                    return new Token { Type = TokenType.LBrace };
                }
                if (ch == '}')
                {
                    return new Token { Type = TokenType.RBrace };
                }
                if (ch == '[')
                {
                    return new Token { Type = TokenType.LSq };
                }
                if (ch == ']')
                {
                    return new Token { Type = TokenType.RSq };
                }

            }
        }

        Token ParseString()
        {
            var nakedString = true;

            var builder = new StringBuilder();

            var c = Peek();


            if ((Char)c == '"')
            {
                nakedString = false;
                Next();
            }

            //c = decoder.Peek();
            //Console.WriteLine("str: {0}", (Char)c);

            var starting = true;

            while (true)
            {
                c = Peek();

                //Console.WriteLine("str: {0}", (Char)c);

                if (c == -1)
                {

                    return new Token { Type = TokenType.String, Value = builder.ToString() };
                }

                var ch = (Char)c;

                if (nakedString && !IsLegalNakedChar(ch, starting))
                {
                    return new Token { Type = TokenType.String, Value = builder.ToString() };
                }

                if (!nakedString)
                {
                    if (ch == '"')
                    {
                        Next();
                        return new Token { Type = TokenType.String, Value = builder.ToString() };
                    }
                }

                // Console.WriteLine("str: {0}", ch);

                builder.Append(ch);

                Next();

                starting = false;
            }

            // return "";
        }

        Token ParseNumber()
        {
            var builder = new StringBuilder();

            var gotPrefix = false;
            Char prefix = ' ';
            var gotExp = false;
            var gotDot = false;

            var c = Peek();

            var ch = (Char)c;

            if (!Char.IsDigit(ch) && ch != '-')
            {
                throw new Exception("Invalid number");
            }

            builder.Append(ch);

            Next();

            c = Peek();

            if (ch == '0' && (Char.ToLower((Char)c) == 'b' || Char.ToLower((Char)c) == 'x' || Char.ToLower((Char)c) == 'o'))
            {
                gotPrefix = true;
                prefix = Char.ToLower((Char)c);
                builder.Append((Char)c);
                Next();
            }

            while (true)
            {
                c = Peek();

                //Console.WriteLine("str: {0}", (Char)c);

                if (c == -1)
                {

                    return new Token { Type = TokenType.Number, Value = builder.ToString() };
                }

                ch = (Char)c;

                if (ch == '-')
                {
                    if (gotPrefix)
                    {
                        throw new Exception("Can't negate prefixed integer");

                    }

                    if (!gotExp)
                    {
                        throw new Exception("Only one negation allowed");
                    }
                }
                else if (gotPrefix && prefix == 'x' && IsHexDigit(ch))
                {

                }
                else if (ch == 'e' || ch == 'E')
                {
                    if (gotPrefix)
                    {
                        throw new Exception("Exponent not allowed in prefixed integer");

                    }
                    if (gotExp)
                    {
                        throw new Exception("Already got exp");
                    }
                    gotExp = true;
                }
                else if (ch == '.')
                {
                    if (gotPrefix)
                    {
                        throw new Exception("Fractional not allowed in prefixed integer");

                    }
                    if (gotExp)
                    {
                        throw new Exception("Fractional Exponents disallowed");

                    }

                    if (gotDot)
                    {
                        throw new Exception("Already got dot");
                    }

                    gotDot = true;
                }
                else if (Char.IsWhiteSpace(ch) || ch == '\n' || ch == ';')
                {
                    return new Token { Type = TokenType.Number, Value = builder.ToString() };
                }
                else if (!Char.IsDigit(ch))
                {
                    throw new Exception("Non-digit in number");

                }

                builder.Append(ch);

                Next();

            }
        }

        public int Peek()
        {

            if (peek != -1)
            {
                return peek;
            }

            var c = reader.Read();
            peek = c;
            return peek;
        }

        public int Next()
        {
            if (peek != -1)
            {
                var tmp = peek;
                peek = -1;
                return tmp;
            }
            return reader.Read();

        }
        bool IsHexDigit(Char ch)
        {
            return ch == 'A' || ch == 'B' || ch == 'C' || ch == 'D' || ch == 'E' || ch == 'F' ||
            ch == 'a' || ch == 'b' || ch == 'c' || ch == 'd' || ch == 'e' || ch == 'f' || Char.IsDigit(ch);
        }
    }

}
