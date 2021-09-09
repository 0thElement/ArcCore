using System.Collections.Generic;
using ArcCore.Serialization;

namespace ArcCore.UI.SongSelection
{
    public interface ISongListDisplayMethod
    {
        List<CellDataBase> FromSongList(List<LevelInfoInternal> toDisplay, GameObject folderPrefab, GameObject cellPrefab, float prioritizedDifficulty);
    }
}