using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pfs.Plex.Model;

namespace Pfs
{
    public static class Utils
    {
        private static readonly Regex _windowsPathRegex = new Regex(@"^[a-zA-Z]:(\\|\/|$)", RegexOptions.Compiled | RegexOptions.Singleline);

        public static void CleanAndDedupe(IEnumerable<Node> items)
        {
            var fileNameCache = new HashSet<string>();
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var item in items)
            {
                var sanitised = new string(item.Name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
                var ext = Path.GetExtension(sanitised);
                var name = Path.GetFileNameWithoutExtension(sanitised);
                var addition = "";
                var i = 0;
                while (fileNameCache.Contains(name + addition + ext))
                {
                    addition = i++ == 0 ? $" - ({item.Id})" : $" - {i}";
                }
                var filename = name + addition + ext;
                fileNameCache.Add(filename);
                item.Name = filename;
            }
        }

        public static async Task<T> FirstSuccessfulTask<T>(IEnumerable<Task<T>> tasks)
        {
            var taskList = tasks.ToList();
            var tcs = new TaskCompletionSource<T>();
            var remainingTasks = taskList.Count;
            foreach (var task in taskList)
            {
                task.ContinueWith(t => {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        tcs.TrySetResult(t.Result);
                    }
                    else if (Interlocked.Decrement(ref remainingTasks) == 0)
                    {
                        tcs.TrySetResult(default(T));
                    }
                });
            }
            return await tcs.Task;
        }

        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            return (long) (TimeZoneInfo.ConvertTimeToUtc(dateTime) - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

        public static DateTime ToDateTime(this long val)
        {
            return DateTimeOffset.FromUnixTimeSeconds(val).DateTime;
        }

        public static DateTime ToDateTime(this string val)
        {
            return ToDateTime(long.Parse(val));
        }

        public class PlexDateTimeConverter : JsonConverter<DateTime>
        {
            public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var inputVal = serializer.Deserialize<long>(reader);
                return inputVal.ToDateTime();
            }

            public override bool CanRead => true;
        }

        public static (string directory, string filename) GetPathInfo(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                path = path.Substring(0, path.Length - 1);
            }

            var i = Math.Max(path.LastIndexOf(Path.DirectorySeparatorChar), path.LastIndexOf(Path.AltDirectorySeparatorChar));
            var folder = i <= 0 ? "/" : path.Substring(0, i);
            var file = i + 2 > path.Length ? "" : path.Substring(i + 1);

            return (folder, file);
        }

        public static string NormalisePath(string path)
        {
            var inputPath = _windowsPathRegex.Replace(path, "\\")
                            .Replace('?', '\n')
                            .Replace('#', '\r');
            return new Uri($"file://{inputPath}")
                .LocalPath
                .Replace("\\", "")
                .Replace('\r', '#')
                .Replace('\n', '?')
                .Trim();
        }
    }
}
