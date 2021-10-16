using System.IO;


namespace Ld2Extractor
{
    /// <summary>
    /// 解析主控逻辑
    /// SrcDir:https://github.com/PurlingNayuki/lingoes-extractor
    /// </summary>
    public class MainWin
    {
        private LingoesLd2Extractor extractor;

        private long lastRun;
        public virtual void setStatusDirect(long total, long finished, int numPerSecond)
        {

        }

        public virtual void setStatus(long total, long finished, int numPerSecond)
        {
            if ((System.DateTime.Now.Millisecond - this.lastRun) > 1000)
            {
                setStatusDirect(total, finished, numPerSecond);
            }
        }

        public void Main(string SrcFile, string TagFile, bool AppendToDict = false)
        {
            this.lastRun = -1L;
            this.extractor = new LingoesLd2Extractor(this);
            this.extractor.Outputer = new Outputer();
            this.extractor.Outputer.OutType = Outputer.OutputType.TextFile;
            if (AppendToDict)
            {
                this.extractor.Outputer.OutType |= Outputer.OutputType.StringDict;
            }
            extractor.extractLd2ToFile(File.OpenRead(SrcFile), File.OpenWrite(TagFile));
        }
        public void Main(string SrcFile)
        {
            this.lastRun = -1L;
            this.extractor = new LingoesLd2Extractor(this);
            this.extractor.Outputer = new Outputer();
            this.extractor.Outputer.OutType = Outputer.OutputType.StringDict;
            extractor.extractLd2ToFile(File.OpenRead(SrcFile), null);
        }

    }
}
