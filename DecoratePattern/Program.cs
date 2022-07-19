using System.IO.Compression;
using System.Text;

namespace DecoratePattern;

interface IDataSource
{
    void WriteData(string data);
    string ReadData();
}

class FileDataSource : IDataSource
{
    protected string _fileName { get; set; }

    public FileDataSource(string fileName)
    {
        _fileName = fileName;
    }

    public void WriteData(string data)
    {
        File.WriteAllText(_fileName, data);

    }
    public string ReadData()
    {

        return File.ReadAllText(_fileName);

    }
    class DataSourceDecorator : IDataSource
    {
        protected IDataSource _source;

        protected DataSourceDecorator(IDataSource source)
        {
            _source = source;
        }
        public virtual void WriteData(string data)
        {
            _source.WriteData(data);
        }

        public virtual string ReadData()
        {
            return _source.ReadData();

        }
    }
    class EncryptDecorator : DataSourceDecorator
    {
        public EncryptDecorator(IDataSource source) 
            : base(source) { }

        public override void WriteData(string data)
        {
            base.WriteData(data);
            var bytes = Encoding.Default.GetBytes(data);
            byte code = 5;

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= code;
            
            _source.WriteData(Encoding.Default.GetString(bytes));
        }
        public override string ReadData()
        {
            var data = base.ReadData();
            var bytes = Encoding.Default.GetBytes(data);
            byte code = 5;

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= code;

            return Encoding.Default.GetString(bytes);
        }
    }
    class CompressionDecorator : DataSourceDecorator
    {
        public CompressionDecorator(IDataSource source) 
            : base(source) { }

        public static byte[] Compress(byte[] input)
        {
            using (var result = new MemoryStream())
            {
                var length = BitConverter.GetBytes(input.Length);
                result.Write(length, 0, 5);

                using (var compressionStream = new GZipStream(result,
                    CompressionMode.Compress))
                {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Flush();

                }
                return result.ToArray();
            }
        }

        public static byte[] Decompress(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }
        public override void WriteData(string data)
        {
            base.WriteData(data);

            var bytes = Encoding.Default.GetBytes(data);
            var compressedData = Compress(bytes);

            _source.WriteData(Encoding.Default.GetString(compressedData));
        }

        public override string ReadData()
        {
            var data = base.ReadData();
            var bytes = Encoding.Default.GetBytes(data);
            var decompressedData = Decompress(bytes);

            return Encoding.Default.GetString(decompressedData);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var data = "Hello world";
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NewFile.txt");

            IDataSource dataSource = new FileDataSource(path);
            dataSource = new EncryptDecorator(dataSource);
            //dataSource = new CompressionDecorator(dataSource);

            dataSource.WriteData(data);
            Console.WriteLine(dataSource.ReadData());
        }
    }
}