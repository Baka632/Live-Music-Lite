using LiveMusicLite.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.Storage;
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
            TileNotification tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }

        /// <summary>
        /// 删除磁贴
        /// </summary>
        public void DeleteTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }
    }
}
