using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace LiveMusicLite.Helper
{
    public class TileHelper
    {
        /// <summary>
        /// 显示磁贴
        /// </summary>
        /// <param name="tileContent">磁贴源</param>
        public void ShowTitle(TileContent tileContent)
        {
            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }

        /// <summary>
        /// 删除磁贴
        /// </summary>
        public void DeleteTile() => TileUpdateManager.CreateTileUpdaterForApplication().Clear();
    }
}
