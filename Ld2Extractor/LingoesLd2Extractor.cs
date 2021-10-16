using BxNiom.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Ld2Extractor
{

    public class Outputer
    {
        [Flags]
        public enum OutputType
        {
            StringDict = 1,
            TextFile = 2
        }
        /// <summary>
        /// 输出类型
        /// </summary>
        public OutputType OutType { get; set; }
        public StreamWriter Writer { get; set; }
        public Dictionary<string, string> Ld2Dict { get; set; }

        public void Output(string Key, string Value)
        {
            if (OutType.HasFlag(OutputType.StringDict))
            {
                Ld2Dict.Add(Key, Value);
            }
            if (OutType.HasFlag(OutputType.TextFile))
            {
                Writer.Write(Key);
                Writer.Write("=");
                Writer.Write(Value);
                Writer.Write("\r\n");
            }
        }

    }
    /// <summary>
    /// 提取器
    /// </summary>
    public class LingoesLd2Extractor
    {
        private bool started;
        /// <summary>
        /// 开始时间:ms
        /// </summary>
        private long startedTime;
        /// <summary>
        /// 至少两个
        /// </summary>
        private static StringDecoderSensitive[] Avail_Encodings = new StringDecoderSensitive[]
        {
            new StringDecoderSensitive(Helper.Charset_UTF8){ name="UTF8"},
            new StringDecoderSensitive(Helper.Charset_UTF16LE){ name="UTF16LE"},
        };

        private static byte[] Transfer_Bytes = new byte[Helper.Buffer_Size];

        private MainWin main;
        public Outputer Outputer { get; set; }
        private int readDictionary(FileStream dataRawBytes, int offsetData)
        {
            int counter;
            dataRawBytes.Position = offsetData + 4;
            int limit = dataRawBytes.ReadInt32() + offsetData + 8;
            int offsetIndex = offsetData + 0x1C;
            dataRawBytes.Position = offsetData + 8;
            int offsetCompressedDataHeader = dataRawBytes.ReadInt32() + offsetIndex;
            int inflatedWordsIndexLength = dataRawBytes.ReadInt32(); // 12
            int inflatedWordsLength = dataRawBytes.ReadInt32(); //16
            List<int> deflateStreams = new List<int>();
            dataRawBytes.Position = (offsetCompressedDataHeader + 8);
            int offset = dataRawBytes.ReadInt32();
            while (this.started && (offset + dataRawBytes.Position < limit))
            {
                offset = dataRawBytes.ReadInt32();
                deflateStreams.Add(offset);
            }
            MemoryStream inflatedBytes = inflate(dataRawBytes, deflateStreams);

            counter = Extract(inflatedBytes, inflatedWordsIndexLength, inflatedWordsIndexLength + inflatedWordsLength);
            return counter;
        }

        private static int calcSpeed(int finished, long started, long now)
        {
            double timeDiff = now - started;
            if (timeDiff > 0)
            {
                double speed = (finished * 1000.0) / timeDiff;
                return (int)speed;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 解压膨胀
        /// </summary>
        /// <param name="dataRawBytes"></param>
        /// <param name="deflateStreams"></param>
        /// <returns></returns>
        private static MemoryStream inflate(FileStream dataRawBytes, List<int> deflateStreams)
        {
            long startOffset = dataRawBytes.Position;
            long offset = -1;
            long lastOffset = startOffset;

            MemoryStream outstm = new MemoryStream();
            foreach (var iterator in deflateStreams)
            {
                offset = startOffset + iterator;
                decompress(outstm, dataRawBytes, lastOffset);
                lastOffset = offset;
            }
            return outstm;
        }

        /// <summary>
        /// 提取
        /// </summary>
        /// <param name="inflatedBytes"></param>
        /// <param name="offsetDefs"></param>
        /// <param name="offsetXml"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private int Extract(MemoryStream inflatedBytes, int offsetDefs, int offsetXml)
        {
            int counter = 0;
            try
            {
                int dataLen = 10;
                int defTotal = offsetDefs / dataLen - 1;
                this.main.setStatus(defTotal, 0L, 0);
                int[] idxData = new int[6];
                string[] defData = new string[2];
                String def;
                StringDecoderSensitive[] encodings;// = Avail_Encodings;
                encodings = this.detectEncodings(inflatedBytes, offsetDefs, offsetXml, defTotal, dataLen, idxData, defData);

                inflatedBytes.Position = 8;
                for (int i = 0; i < defTotal; i++)
                {
                    readDefinitionData(inflatedBytes, offsetDefs, offsetXml, dataLen, encodings[0], encodings[1], idxData, defData, i);
                    if (Helper.DEBUG)
                    {
                        def = defData[0] + "=" + defData[1];
                        Console.WriteLine($"{i}:{def}");
                    }
                    this.Outputer.Output(defData[0], defData[1]);
                    counter++;
                    this.main.setStatus(defTotal, i, calcSpeed(i, startedTime, DateTime.Now.Millisecond));
                }
                Console.WriteLine("OK");
            }
            finally
            {
                this.Outputer.Writer?.Close();
            }
            return counter;
        }
        /// <summary>
        /// 探测编码
        /// </summary>
        /// <param name="inflatedBytes"></param>
        /// <param name="offsetWords"></param>
        /// <param name="offsetXml"></param>
        /// <param name="defTotal"></param>
        /// <param name="dataLen"></param>
        /// <param name="idxData"></param>
        /// <param name="defData"></param>
        /// <returns></returns>
        private StringDecoderSensitive[] detectEncodings(MemoryStream inflatedBytes,
            int offsetWords, int offsetXml, int defTotal, int dataLen, int[] idxData, string[] defData)
        {

#if true  // 关闭编码探测
            int test = Math.Min(defTotal, 500);
            //Regex p = new Regex("^.*[\\x00-\\x1f].*$");
            StringDecoderSensitive[] avail = Avail_Encodings;

            for (int i = 0; i < avail.Length; i++)
            {
                StringDecoderSensitive element = avail[i];
                for (int j = 0; j < avail.Length; j++)
                {
                    StringDecoderSensitive element2 = avail[j];
                    try
                    {
                        this.readDefinitionData(inflatedBytes, offsetWords, offsetXml, dataLen, element, element2, idxData, defData, test);
                        Console.WriteLine("词组编码：" + element.name);
                        Console.WriteLine("XML编码：" + element2.name);
                        return new StringDecoderSensitive[] { element, element2 };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            Console.WriteLine("自动识别编码失败！选择UTF-16LE继续。");
            return new StringDecoderSensitive[] { Avail_Encodings[1], Avail_Encodings[1] };
#else
            return return new StringDecoderSensitive[] { Avail_Encodings[0], Avail_Encodings[0] };
#endif
        }
        /// <summary>
        /// 读取定义数据
        /// 先读Values,再读Key
        /// </summary>
        /// <param name="inflatedBytes"></param>
        /// <param name="offsetWords"></param>
        /// <param name="offsetXml"></param>
        /// <param name="dataLen"></param>
        /// <param name="wordDecoder"></param>
        /// <param name="valueDecoder"></param>
        /// <param name="wordIdxData"></param>
        /// <param name="wordData"></param>
        /// <param name="idx"></param>
        private void readDefinitionData(MemoryStream inflatedBytes,
            int offsetWords, int offsetXml, int dataLen,
            StringDecoderSensitive wordDecoder,
            StringDecoderSensitive valueDecoder,
            int[] wordIdxData, string[] wordData, int idx)
        {
            getIdxData(inflatedBytes, dataLen * idx, wordIdxData);
            int lastWordPos = wordIdxData[0];
            int lastXmlPos = wordIdxData[1];
            int wc = 0;
            int refs = wordIdxData[3]; // 多义/引用
            int currentWordOffset = wordIdxData[4];
            int currenXmlOffset = wordIdxData[5];
            wc = refs;

            C strip1;
            string strip2_key;
            C strip3;
            C strip4;
            List<string> Values = new List<string>();


            inflatedBytes.Position = 0;
            string xml = valueDecoder.decode(inflatedBytes, offsetXml + lastXmlPos, currenXmlOffset - lastXmlPos);
            strip1 = strip(xml);

            if (strip1?.F?.I?.N.Q != null)
                Values.Add(strip1.F.I.N.Q);

            while (this.started && (refs-- > 0))
            {
                inflatedBytes.Position = offsetWords + lastWordPos;
                int ref2 = inflatedBytes.ReadInt32();
                getIdxData(inflatedBytes, dataLen * ref2, wordIdxData);
                lastXmlPos = wordIdxData[1];
                currenXmlOffset = wordIdxData[5];

                if (string.IsNullOrEmpty(wordData[1]))
                {
                    strip3 = strip(valueDecoder.decode(inflatedBytes, offsetXml + lastXmlPos, currenXmlOffset - lastXmlPos));
                    if (strip3?.F?.I?.N.Q != null)
                        Values.Add(strip3.F.I.N.Q);
                }
                else
                {
                    strip4 = strip(valueDecoder.decode(inflatedBytes, offsetXml + lastXmlPos, currenXmlOffset - lastXmlPos));
                    if (strip4?.F?.I?.N.Q != null)
                        Values.Add(strip4.F.I.N.Q);
                }
                lastWordPos += 4;
            }
            strip2_key = wordDecoder.decode(inflatedBytes, offsetWords + lastWordPos, currentWordOffset - lastWordPos);

            wordData[0] = strip2_key;
            wordData[1] = string.Join(Helper.SEP_List, Values);
            if (Helper.DEBUG && (string.IsNullOrEmpty(wordData[0]) || string.IsNullOrEmpty(wordData[1])))
            {
                Console.WriteLine("??");
                Console.WriteLine(wordData[0] + " = " + wordData[1]);
            }
        }

        private static void getIdxData(MemoryStream dataRawBytes, int position, int[] wordIdxData)
        {
            dataRawBytes.Position = position;
            wordIdxData[0] = dataRawBytes.ReadInt32();
            wordIdxData[1] = dataRawBytes.ReadInt32();
            wordIdxData[2] = dataRawBytes.ReadByte() & 0xff;
            wordIdxData[3] = dataRawBytes.ReadByte() & 0xff;
            wordIdxData[4] = dataRawBytes.ReadInt32();
            wordIdxData[5] = dataRawBytes.ReadInt32();
        }

        /// <summary>
        /// XML外壳C
        /// </summary>
        public class C
        {
            public F F;
        }
        public class F
        {
            public H H;
            public I I;
        }
        public class H
        {
            public string M;
            public H()
            {
                M = string.Empty;
            }
        }
        public class N
        {
            public string Q;
            public N()
            {
                Q = string.Empty;
            }
        }
        public class I
        {
            public N N;
        }

        /// <summary>
        /// 剥去XML外壳
        /// </summary>
        /// <param name="xml"></param>
        /// <code>
        /// C.F.H.M
        /// C.F.I.N.Q
        /// <C>
        ///     <F>
        ///         <H><M>ei, ə, æn, ən</M></H>
        ///         <I><N><Q>art.一(个)；任何一(个)；每一(个)</Q></N></I>
        ///     </F>
        /// </C>
        /// </code>
        /// <returns></returns>
        private static C strip(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml) == true)
                return new C();
            XmlSerializer serializer = new XmlSerializer(typeof(C));
            //反序列化，并将反序列化结果值赋给变量obj
            C obj = (C)serializer.Deserialize(new MemoryStream(UTF8Encoding.UTF8.GetBytes(xml)));
            return obj;
        }
        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="outmem"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static long decompress(MemoryStream outmem, FileStream data, long offset)
        {
            Inflater inflater = new Inflater();
            data.Position = offset;
            InflaterInputStream instm = new InflaterInputStream(data, inflater, Helper.Buffer_Size);
            long bytesRead = -1;
            try
            {
                writeInputStream(instm, outmem);
                bytesRead = inflater.TotalOut;
            }
            finally
            {
                // instm.Close(); => data is CLosed
            }
            return bytesRead;
        }

        private static void writeInputStream(InflaterInputStream instm, MemoryStream outstm)
        {
            int num;
            while ((num = instm.Read(Transfer_Bytes)) > 0)
            {
                outstm.Write(Transfer_Bytes, 0, num);
            }
        }

        public LingoesLd2Extractor(MainWin main)
        {
            this.started = false;
            this.main = main;
        }

        private void InitOutputer(FileStream outFile)
        {
            if (Outputer.OutType.HasFlag(Outputer.OutputType.TextFile))
                Outputer.Writer = new StreamWriter(outFile);
            if (Outputer.OutType.HasFlag(Outputer.OutputType.StringDict))
                Outputer.Ld2Dict = new Dictionary<string, string>();
        }
        private void InitOutputer()
        {
            if (Outputer.OutType.HasFlag(Outputer.OutputType.StringDict))
                Outputer.Ld2Dict = new Dictionary<string, string>();
        }

        public virtual int extractLd2ToFile(FileStream ld2File, FileStream outFile)
        {
            this.started = true;
            this.startedTime = DateTime.Now.Millisecond;
            FileStream dataRawBytes = ld2File;
            InitOutputer(outFile);
            int counter = 0;

            if (dataRawBytes != null)
            {
                dataRawBytes.Position = 0x5C;
                int offsetData = dataRawBytes.ReadInt32() + 0x60;
                if (dataRawBytes.Length > offsetData)
                {
                    dataRawBytes.Position = offsetData;
                    int type = dataRawBytes.ReadInt32();
                    dataRawBytes.Position = offsetData + 4;
                    int offsetWithInfo = dataRawBytes.ReadInt32() + offsetData + 12;
                    if (type == 3)
                    {
                        counter = readDictionary(dataRawBytes, offsetData);
                    }
                    else if (dataRawBytes.Length > (offsetWithInfo + 0x1C))
                    {
                        counter = readDictionary(dataRawBytes, offsetWithInfo);
                    }
                    else
                    {
                        Console.WriteLine("文件不包含字典数据。网上字典？");
                    }
                }
                else
                {
                    Console.WriteLine("文件不包含字典数据。网上字典？");
                }
                return counter;
            }
            Console.WriteLine("文件不包含字典数据。网上字典？");
            return 0;
        }

        public virtual void cancel()
        {
            this.started = false;
        }
    }
}
