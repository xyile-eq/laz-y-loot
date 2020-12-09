using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LazLootIni
{
    public class ItemIconInfo
    {
        public string Name { get; set; }
        public int ItemId { get; set; }
        public int? IconId { get; set; }

        [JsonIgnore]
        public object IconUri
        {
            get
            {
                if (IconId == null)
                    return null;

                object icon = default(object);
                if (IconId.HasValue)
                    icon = LocalItemDB.GetItemIcon(IconId.Value);

                if (icon != null)
                    return icon;
                else
                {
                    BitmapImage tmp = new BitmapImage();
                    tmp.BeginInit();
                    tmp.UriSource = new Uri($"https://www.magelocdn.com/images/eq/item_icones/item_{IconId}.png?v=-1", UriKind.Absolute);
                    tmp.EndInit();
                    return tmp;
                }
            }
        }

        public override string ToString()
        {
            return $"{Name} (ID:{ItemId})";
        }
    }
}
