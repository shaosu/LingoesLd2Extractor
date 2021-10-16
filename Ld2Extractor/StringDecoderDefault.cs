using BxNiom.IO;
using System;
using System.IO;
using System.Text;

namespace Ld2Extractor
{
    public  class StringDecoderDefault
    {
        public String name;
        protected System.Text.Decoder cd;
        public StringDecoderDefault(Decoder cs)
        {
            this.cd = cs;
        }
        public string decode(MemoryStream mem, int off, int len)
        {
            if (len == 0)
                return "";
            cd.Reset();
            mem.Position = off;
            byte[] ba = mem.ReadBytes(len);
            int charSize = cd.GetCharCount(ba, 0, len);
            Char[] chs = new char[charSize];
            int c = cd.GetChars(ba, 0, len, chs, 0);
            return new string(chs);
        }
        public virtual string decode(byte[] ba, int off, int len)
        {
            if (len == 0)
                return "";
            cd.Reset();
            int charSize = cd.GetCharCount(ba, off, len);
            Char[] chs = new char[charSize];
            int c = cd.GetChars(ba, off, len, chs, 0);
            return new string(chs);
        }

        /*
public char[] decode(byte[] ba, int off, int len)
{
    int en = (int)(len * (double)cd.maxCharsPerByte());
    char[] ca = new char[en];
    if (len == 0)
        return ca;
    cd.reset();
    ByteBuffer bb = ByteBuffer.wrap(ba, off, len);
    CharBuffer cb = CharBuffer.wrap(ca);
    try
    {
        CoderResult cr = cd.decode(bb, cb, true);
        if (!cr.isUnderflow())
        {
            cr.throwException();
        }
        cr = cd.flush(cb);
        if (!cr.isUnderflow())
        {
            cr.throwException();
        }
    }
    catch (CharacterCodingException x)
    {
        throw new Error(x);
    }
    return safeTrim(ca, cb.position());
}
*/
    }

}
