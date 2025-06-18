using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace MagitekStratagemPlugin
{
    public class GameObjectHeatmapService : IDisposable
    {
        private readonly Dictionary<IntPtr, float> heatMap = new();

        public IDictionary<IntPtr, float> HeatMap => heatMap;

        public Configuration Configuration { get; }

        public GameObjectHeatmapService(Configuration configuration)
        {
            Configuration = configuration;
        }

        public void AddOrUpdate(IntPtr key, float value)
        {
            if (heatMap.ContainsKey(key))
                heatMap[key] = value;
            else
                heatMap.Add(key, value);
        }

        public unsafe void UpdateHeatMap(GameObjectStruct* gameObject)
        {
            if (gameObject != null)
            {
                if (heatMap.ContainsKey((IntPtr)gameObject))
                {
                    heatMap[(IntPtr)gameObject] += Configuration.HeatIncrement;
                }
                else
                {
                    heatMap[(IntPtr)gameObject] = Configuration.HeatIncrement;
                }
            }
        }

        public void Remove(IntPtr key)
        {
            heatMap.Remove(key);
        }

        public void DecayHeat(float decay)
        {
            var keys = heatMap.Keys.ToList();
            foreach (var key in keys)
            {
                heatMap[key] = heatMap[key] * decay;
                if (heatMap[key] < 0.01f)
                {
                    heatMap.Remove(key);
                }
            }
        }

        public IntPtr? FindMaxHeat()
        {
            if (heatMap.Keys.Count > 1)
            {
                return heatMap.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            }
            else if (heatMap.Keys.Count == 1)
            {
                return heatMap.Keys.First();
            }
            else
            {
                return null;
            }
        }

        public void Clear()
        {
            heatMap.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
