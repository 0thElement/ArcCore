// #define DUMMY_DATA

using UnityEngine;
using UnityEngine.UI;
using ArcCore.Storage.Data;
using System.Collections.Generic;
using System.Linq;
using ArcCore.Storage;

namespace ArcCore.UI.SongSelection
{
    public class SelectionMenu : MonoBehaviour
    {
        public static SelectionMenu Instance;
        protected void Awake()
        {
            Instance = this;
            GetData();
            Display();
        }
        
        private const string lastSelectedPackPref = "LastSelectedPack";
        private const string lastSelectedSongPref = "LastSelectedLevel";
        private const string lastSelectedDiffPref = "LastSelectedDiff";

        [SerializeField] public PackListDisplay packList;
        [SerializeField] public LevelListDisplay levelList;
        [SerializeField] public LevelInfoDisplay levelInfo;
        [SerializeField] public DifficultyListDisplay diffList;

        private List<Pack> packsData => FileManagement.packs;
        private List<Level> levelsData => FileManagement.levels;

        private Level selectedLevel;
        private Pack selectedPack;
        private DifficultyGroup selectedDiff;

        public delegate void OnPackChangeDelegate(Pack pack);
        public OnPackChangeDelegate OnPackChange;
        public delegate void OnLevelChangeDelegate(Level level);
        public OnLevelChangeDelegate OnLevelChange;
        public delegate void OnDifficultyChangeDelegate(DifficultyGroup difficultyGroup);
        public OnDifficultyChangeDelegate OnDifficultyChange;

        public Pack SelectedPack
        {
            get => selectedPack;
            set {
                if (selectedPack != value)
                {
                    selectedPack = value;
                    //TODO: Save the last selected pack
                    if (OnPackChange != null) OnPackChange(value);

                    //TODO: Get the last selected level of the pack
                    SelectedLevel = null;
                    DrawLevels();
                }
            }
        }

        public Level SelectedLevel
        {
            get => selectedLevel;
            set {
                if (selectedLevel != value)
                {
                    selectedLevel = value;

                    if (SelectedLevel != null)
                    {
                        bool diffIncluded = false;
                        foreach (Chart chart in value.Charts)
                        {
                            if (chart.DifficultyGroup == selectedDiff) diffIncluded = true;
                        }
                        if (!diffIncluded) selectedDiff = value.GetClosestChart(selectedDiff).DifficultyGroup;
                    }

                    //TODO: Save the last selected level (per pack)
                    if (OnLevelChange != null) OnLevelChange(value);
                    DrawDifficulties();
                    DrawInfo();
                }
            }
        }

        public DifficultyGroup SelectedDiff
        {
            get => selectedDiff;
            set {
                if (SelectedDiff != value)
                {
                    selectedDiff = value;
                    //TODO: Save the last selected difficulty group (globally)
                    if (OnDifficultyChange != null) OnDifficultyChange(value);
                    DrawLevels();
                    DrawDifficulties();
                    DrawInfo();
                }
            }
        }

        private void GetData()
        {
#if DUMMY_DATA
            DifficultyGroup pst = new DifficultyGroup() {
                Color = Color.cyan,
                Name = "Past",
                Precedence = 0
            };
            DifficultyGroup prs = new DifficultyGroup() {
                Color = Color.green,
                Name = "Present",
                Precedence = 10
            };
            DifficultyGroup ftr = new DifficultyGroup() {
                Color = new Color(1,0,1,1),
                Name = "Future",
                Precedence = 20
            };
            DifficultyGroup byd = new DifficultyGroup() {
                Color = Color.red,
                Name = "Beyond",
                Precedence = 30
            };
            ChartSettings defaultSettings = new ChartSettings() {
                Offset = 0,
                ChartSpeed = 1
            };
            selectedDiff = pst;

            FileManagement.packs = new List<Pack>() {
                new Pack() {
                    Id = 0,
                    Name = "A"
                },
                new Pack() {
                    Id = 1,
                    Name = "B"
                },
                new Pack() {
                    Id = 2,
                    Name = "C"
                }
            };

            FileManagement.levels = new List<Level>() {
                new Level {
                    Id = 1,
                    Charts = new Chart[] {
                        new Chart() {
                            DifficultyGroup = pst,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song1",
                            NameRomanized = "Song1",
                            Artist = "Artist1",
                            ArtistRomanized = "Artist1",
                            Bpm = "100",
                            Constant = 4,
                            PbScore = 10000255,
                            PbGrade = ScoreCategory.PureMemory,
                            Settings = defaultSettings,
                            Charter = "Charter1"
                        },
                        new Chart() {
                            DifficultyGroup = prs,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song1",
                            NameRomanized = "Song1",
                            Artist = "Artist1",
                            ArtistRomanized = "Artist1",
                            Bpm = "100",
                            Constant = 6,
                            PbScore = 9986051,
                            PbGrade = ScoreCategory.FullRecall,
                            Settings = defaultSettings,
                            Charter = "Charter2"
                        },
                        new Chart() {
                            DifficultyGroup = ftr,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song1",
                            NameRomanized = "Song1",
                            Artist = "Artist1",
                            ArtistRomanized = "Artist1",
                            Bpm = "100",
                            Constant = 8,
                            PbScore = 9800000,
                            PbGrade = ScoreCategory.NormalClear,
                            Settings = defaultSettings,
                            Charter = "Charter3"
                        },
                        new Chart() {
                            DifficultyGroup = byd,
                            SongPath = "/data/test1/remix.ogg",
                            ImagePath = "/data/test1/remix.jpg",
                            Name = "Song1 remix",
                            NameRomanized = "Song1 remix",
                            Artist = "Artist1 but cooler",
                            ArtistRomanized = "Artist1 but cooler",
                            Bpm = "150",
                            Constant = 10.8f,
                            PbScore = 9500000,
                            PbGrade = ScoreCategory.NormalClear,
                            Settings = defaultSettings,
                            Charter = "Charter3"
                        }
                    },
                    Pack = FileManagement.packs[0]
                },
                new Level {
                    Id = 0,
                    Charts = new Chart[] {
                        new Chart() {
                            DifficultyGroup = pst,
                            SongPath = "/data/test2/base.ogg",
                            ImagePath = "/data/test2/base.jpg",
                            Name = "Song2",
                            NameRomanized = "Song2",
                            Artist = "Artist2",
                            ArtistRomanized = "Artist2",
                            Bpm = "100",
                            Constant = 4,
                            PbScore = 10000255,
                            PbGrade = ScoreCategory.PureMemory,
                            Settings = defaultSettings,
                            Charter = "Charter5"
                        },
                        new Chart() {
                            DifficultyGroup = prs,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song2",
                            NameRomanized = "Song2",
                            Artist = "Artist2",
                            ArtistRomanized = "Artist2",
                            Bpm = "100",
                            Constant = 6,
                            PbScore = 9986051,
                            PbGrade = ScoreCategory.FullRecall,
                            Settings = defaultSettings,
                            Charter = "Charter6"
                        },
                        new Chart() {
                            DifficultyGroup = ftr,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song2",
                            NameRomanized = "Song2",
                            Artist = "Artist2",
                            ArtistRomanized = "Artist2",
                            Bpm = "100",
                            Constant = 8,
                            PbScore = 9800000,
                            PbGrade = ScoreCategory.NormalClear,
                            Settings = defaultSettings,
                            Charter = "Charter5"
                        },
                        new Chart() {
                            DifficultyGroup = byd,
                            SongPath = "/data/test1/remix.ogg",
                            ImagePath = "/data/test1/remix.jpg",
                            Name = "Song2 remix",
                            NameRomanized = "Song2 remix",
                            Artist = "Artist2 but cooler",
                            ArtistRomanized = "Artist2 but cooler",
                            Bpm = "150",
                            Constant = 10f,
                            PbScore = 9500000,
                            PbGrade = ScoreCategory.NormalClear,
                            Settings = defaultSettings,
                            Charter = "Charter5"
                        }
                    },
                    Pack = FileManagement.packs[1]
                }
            };
#else
            foreach (Pack pack in packsData)
            {
                print(pack.Name);
            }
            foreach (Level level in levelsData)
            {
                print(level.Pack);
            }
#endif
        }

        public void Display()
        {
            //TODO: Get the last selected pack, level, difficulty
            //Stored in some place i dont remember where :)

            DrawPacks();
            DrawLevels();
            DrawDifficulties();
        }

        private void DrawPacks()
        {
            packList.Display(packsData, levelsData);
        }

        public void DrawLevels()
        {

            List<Level> levels = levelsData;
            if (SelectedPack != null) 
            {
                levels = levels.Where(level => level.Pack != null && level.Pack.Id == selectedPack.Id).ToList();
            }

            if (levels.Count == 0) throw new System.Exception("Level list is empty");

            int min = int.MaxValue;
            DifficultyGroup closestDiff = null;
            foreach (Level level in levels)
            {
                DifficultyGroup diff = level.GetClosestChart(selectedDiff).DifficultyGroup;
                if (diff.Precedence - selectedDiff.Precedence < min)
                {
                    min = diff.Precedence - selectedDiff.Precedence;
                    closestDiff = diff;
                }
            }
            selectedDiff = closestDiff;

            levelList.Display(levels, selectedLevel, selectedDiff);
        }

        public void DrawDifficulties()
        {
            if (SelectedLevel != null) 
            {
                List<Chart> charts = selectedLevel.Charts.ToList();
                diffList.Display(charts, selectedDiff);
            }
            else
                diffList.Reset();
        }

        public void DrawInfo()
        {
            levelInfo.Display(selectedLevel, selectedDiff);
        }

        public void CycleDifficulty()
        {
            DifficultyGroup nextGroup = diffList.NextGroup;
            SelectedDiff = nextGroup;
        }
    } 
}