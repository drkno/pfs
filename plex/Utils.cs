using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pfs.Plex
{
    public static class Utils
    {
        public static void CleanAndDedupe(IEnumerable<BaseNode> items)
        {
            var fileNameCache = new HashSet<string>();
            foreach (var item in items) {
                var sanitised = item.Name.Replace('/', '_').Replace('\\', '_');
                var ext = Path.GetExtension(sanitised);
                var name = Path.GetFileNameWithoutExtension(sanitised);
                var addition = "";
                var i = 0;
                while (fileNameCache.Contains(name + addition + ext)) {
                    if (i++ == 0) {
                        addition = $" - ({item.Id})";
                    }
                    else {
                        addition = $" - {i}";
                    }
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
            int remainingTasks = taskList.Count;
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
    }
}
