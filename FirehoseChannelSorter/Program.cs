using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvtSource;
using Newtonsoft.Json;

namespace FirehoseChannelSorter
{
    class Program
    {
        static readonly Dictionary<string, int> channelCounts = new Dictionary<string, int>();
        static readonly Dictionary<string, int> userCounts = new Dictionary<string, int>();
        static volatile bool active = true;

        async static Task<int> Main(string[] args)
        {
            string url = Environment.GetEnvironmentVariable("URL");

            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Please specify URL environment variable");
                return 2;
            }

            int duration = 30;
            if (args.Length > 0 && int.TryParse(args[0], out int d) && d > 0 && d <= 86400)
            {
                duration = d;
            }

            int lines = 25;
            if (args.Length > 1 && int.TryParse(args[1], out int l) && l > 0)
            {
                lines = l;
            }

            bool repeat;
            do
            {
                channelCounts.Clear();
                userCounts.Clear();
                active = true;
                using (var evt = new EventSourceReader(new Uri(url)))
                {
                    evt.MessageReceived += Evt_MessageReceived;
                    evt.Start();

                    Console.WriteLine($"Reading for {duration} seconds...");
                    await Task.Delay(duration * 1000);

                    active = false;
                    Console.WriteLine("Deactivating...");
                }

                await Task.Delay(500);
                PrintTop(channelCounts, lines);
                PrintTop(userCounts, lines);
                Console.Write("Repeat? [y/N] ");
                char c = Console.ReadKey().KeyChar;
                switch (c)
                {
                    case 'y':
                    case 'Y':
                        repeat = true;
                        break;
                    default:
                        repeat = false;
                        break;
                }
                Console.WriteLine();
            }
            while (repeat);

            return 0;
        }

        private static void PrintTop(Dictionary<string, int> counts, int limit)
        {
            Console.WriteLine("messages: name");
            var channels = new Dictionary<int, List<string>>();
            var cs = new List<int>();
            foreach (KeyValuePair<string, int> c in counts)
            {
                if (!cs.Contains(c.Value))
                {
                    cs.Add(c.Value);
                }
                List<string> ch = channels.GetValueOrDefault(c.Value, new List<string>());
                ch.Add(c.Key);
                channels[c.Value] = ch;
            }
            cs.Sort();
            int printed = 0;
            for (int i = cs.Count - 1; i > 0; i--)
            {
                int index = cs[i];
                channels[index].Sort();
                foreach (string channel in channels[index])
                {
                    Console.WriteLine($"{index,8}: {channel}");
                    printed++;
                    if (printed == limit)
                    {
                        goto end;
                    }
                }
            }
        end:
            return;
        }

        private static void Evt_MessageReceived(object sender, EventSourceMessageEventArgs e)
        {
            if (active)
            {
                Privmsg msg = JsonConvert.DeserializeObject<Privmsg>(e.Message);
                int count = channelCounts.GetValueOrDefault(msg.Channel, 0);
                channelCounts[msg.Channel] = count + 1;
                if (!string.IsNullOrEmpty(msg.User))
                {
                    count = userCounts.GetValueOrDefault(msg.User, 0);
                    userCounts[msg.User] = count + 1;
                }
            }
        }

    }
}
