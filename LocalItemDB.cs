﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LazLootIni
{
    static public class LocalItemDB
    {
        public const string ItemDbJsonPath = @"assets\itemdb.json";
        public const string IconDatPath = @"assets\icons.dat";
        public const string MappingsPath = @"assets\mappings.json";
        public const string ItemValuesPath = @"assets\itemvalues.dat";

        public static List<ItemIconInfo> Instance { get; internal set; }
        public static FileStream IconPack { get; set; }
        public static Dictionary<int, dynamic> IconMappings { get; set; }

        static public async Task<List<ItemIconInfo>> LoadAsync()
        {

            if (!File.Exists(ItemDbJsonPath) || !File.Exists(IconDatPath) || !File.Exists(MappingsPath))
            {
                var task = Task.Factory.StartNew(() =>
                {
                    DownloadAsync();
                });
                await task;
                if (task.Exception != null)
                    throw task.Exception;
            }


            var ret = new List<ItemIconInfo>();

            try
            {
                using (var sr = new StreamReader(File.OpenRead(ItemDbJsonPath)))
                {
                    var json = await sr.ReadToEndAsync();
                    ret.AddRange(JsonConvert.DeserializeObject<ItemIconInfo[]>(json));
                }
            }
            catch (Exception)
            {

            }

            IconPack = new FileStream(IconDatPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            IconMappings = new Dictionary<int, dynamic>();
            dynamic mappings = JsonConvert.DeserializeObject(File.ReadAllText(MappingsPath));
            foreach (var item in mappings)
            {
                IconMappings.Add((int)item.ID, (dynamic)item);
            }

            return ret;
        }

        static public object GetItemIcon(int iconId)
        {
            if (LocalItemDB.IconPack != null && iconId > 0)
            {
                dynamic info;
                if (LocalItemDB.IconMappings.TryGetValue(iconId, out info))
                {
                    var offset = Convert.ToInt64(info.Offset);
                    var length = Convert.ToInt32(info.Length);

                    LocalItemDB.IconPack.Seek(offset, SeekOrigin.Begin);
                    var data = new byte[length];

                    LocalItemDB.IconPack.Read(data, 0, length);

                    using (var ms = new MemoryStream(data))
                    {
                        return BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
            }
            BitmapImage tmp = new BitmapImage();
            tmp.BeginInit();
            tmp.UriSource = new Uri($"https://www.magelocdn.com/images/eq/item_icones/item_{iconId}.png?v=-1", UriKind.Absolute);
            tmp.EndInit();
            return tmp;
        }

        static public async void DownloadAsync()
        {
            if (!File.Exists(ItemDbJsonPath))
            {
                using (var wc = new WebClient())
                {
                    var data = await wc.DownloadDataTaskAsync(new Uri("https://s3.us-east-2.amazonaws.com/mamfiles/itemdb.json"));
                    File.WriteAllBytes(ItemDbJsonPath, data);


                }
            }

            if (!File.Exists(IconDatPath))
            {
                using (var wc = new WebClient())
                {
                    var icons = await wc.DownloadDataTaskAsync(new Uri("https://s3.us-east-2.amazonaws.com/mamfiles/icons.dat"));
                    File.WriteAllBytes(IconDatPath, icons);
                }
            }

            if (!File.Exists(MappingsPath))
            {
                using (var wc = new WebClient())
                {
                    var mappings = await wc.DownloadDataTaskAsync(new Uri("https://s3.us-east-2.amazonaws.com/mamfiles/mappings.json"));
                    File.WriteAllBytes(MappingsPath, mappings);
                }
            }

            await LoadAsync();
        }

    }
}
