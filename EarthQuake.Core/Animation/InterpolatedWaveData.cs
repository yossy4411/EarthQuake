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
        public float Start { get; }
        public int End { get; }
        private readonly float last;
        private readonly float[] values;
        private readonly float lastInclination;
        public InterpolatedWaveData(Array data)
        {
            Start = (float)((double?)data.GetValue(0) ?? 0);
            double?[] doubles = new double?[data.Length];
            data.CopyTo(doubles, 0);
            float[] data2 = [..from d in doubles where d.HasValue && d!.Value != 0 select (float)d!.Value];
            values = data2[2..];
            lastInclination =  (data2[^1] - data2[^2]) / 4;
            last = data2[^1];
            End = values.Length - 1;
        }
        public double GetRadius(double seconds)
        {
            double secondsPass = seconds - Start;
            int index = (int)(secondsPass * 4);
            double result;
            if (index == -1)
            {
                result = values[0] * 4 * (secondsPass % 0.25f);
            }
            else if (seconds <= Start) return 0;
            else if (index >= End)
            {
                double index2 = index - End;
                result = last + index2 * lastInclination;
            }
            else
            {
                
                float value = values[index];
                float next = index == 0 ? 0 : values[index + 1];
                result = value * (1 - 4 * (secondsPass % 0.25f)) + next * 4 * (secondsPass % 0.25f);

            }
            return result / Earth * 360;
        }
        public const int Earth = 40075;
    }
    public class PSWave : Dictionary<int, InterpolatedWaveData>
    {
        public static async Task<PSWave> LoadAsync(Stream stream)
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
}
