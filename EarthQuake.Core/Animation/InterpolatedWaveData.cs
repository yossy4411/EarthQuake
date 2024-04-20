using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Parquet.Serialization;
using System.Diagnostics;
using System.IO;

namespace EarthQuake.Core.Animation
{
    public class InterpolatedWaveData
    {
        public double Start { get; }
        public double End { get; }
        public double[] Values { get; }
        public InterpolatedWaveData(Array data)
        {
            double?[] doubles = new double?[data.Length];
            data.CopyTo(doubles, 0);
            double[] data2 = [..from d in doubles where d.HasValue select d!.Value];
            Start = data2[0];
            End = data2[1];
            Values = data2[2..];
        }
        public static async Task<PSWave> Load(Stream stream)
        {
            PSWave values = [];
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream);
            using ParquetReader reader = await ParquetReader.CreateAsync(memoryStream);
            for (int i = 0; i < reader.RowGroupCount; i++)
            {
                using ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i);

                foreach (DataField df in reader.Schema.GetDataFields())
                {
                    DataColumn columnData = await rowGroupReader.ReadColumnAsync(df);
                    values.Add(int.Parse(columnData.Field.Name), new InterpolatedWaveData(columnData.Data));
                }
            }
            return values;
        }
    }
    public class PSWave : Dictionary<int, InterpolatedWaveData>
    {
        public const double EarthRadius = 6367;
    }
}
