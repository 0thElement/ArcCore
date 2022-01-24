using UnityEngine;
using UnityEngine.UI;
using ArcCore.UI.Data;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.UI.SongSelection
{
    public class SelectionMenu : ScrollRect
    {
        public static SelectionMenu Instance;
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            GetData();
        }
        
        private const string lastSelectedPackPref = "LastSelectedPack";
        private const string lastSelectedSongPref = "LastSelectedLevel";
        private const string lastSelectedDiffPref = "LastSelectedDiff";

        [SerializeField] public LevelListDisplay levelList;
        [SerializeField] public PackListDisplay packList;
        [SerializeField] public DifficultyListDisplay diffList;

        [HideInInspector] public List<Level> levelsData;
        [HideInInspector] public List<Pack> packsData;

        private Level selectedLevel;
        private Pack selectedPack;
        private DifficultyGroup selectedDiff;

        public Pack SelectedPack
        {
            get => selectedPack;
            set {
                selectedPack = value;
                //TODO FOR FLOOF:
                //Save the last selected pack
                Draw();
            }
        }

        public Level SelectedLevel
        {
            get => selectedLevel;
            set {
                selectedLevel = value;
                //TODO FOR FLOOF:
                //Save the last selected level (per pack)
                Draw();
            }
        }

        public DifficultyGroup SelectedDiff
        {
            get => selectedDiff;
            set {
                selectedDiff = value;
                //TODO FOR FLOOF:
                //Save the last selected difficulty group (globally)
                Draw();
            }
        }

        private void GetData()
        {
            DifficultyGroup pst = new DifficultyGroup() {
                Color = new Color(),
                Name = "Past",
                Precedence = 0
            };
            DifficultyGroup prs = new DifficultyGroup() {
                Color = new Color(),
                Name = "Present",
                Precedence = 10
            };
            DifficultyGroup ftr = new DifficultyGroup() {
                Color = new Color(),
                Name = "Future",
                Precedence = 20
            };
            DifficultyGroup byd = new DifficultyGroup() {
                Color = new Color(),
                Name = "Beyond",
                Precedence = 30
            };
            ChartSettings defaultSettings = new ChartSettings() {
                Offset = 0,
                ChartSpeed = 1
            };

            packsData = new List<Pack>() {
                new Pack() {

                },
                new Pack() {

                },
                new Pack() {

                }
            };

            levelsData = new List<Level>() {
                new Level {
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
                    Pack = null
                },
                new Level {
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
                            Constant = 10.8f,
                            PbScore = 9500000,
                            PbGrade = ScoreCategory.NormalClear,
                            Settings = defaultSettings,
                            Charter = "Charter5"
                        }
                    },
                    Pack = null
                }
            };
        }

        public void Display()
        {
            //TODO FOR FLOOF
            //Get the last selected pack, level, difficulty
            Draw();
        }

        private void Draw()
        {
            packList.Display(packsData, levelsData, selectedPack);

            List<Level> levels = levelsData;
            if (selectedLevel != null) {
                levels = levelsData.Where(level => level.Pack == null && level.Pack.Id == selectedPack.Id).ToList();
            }
            levelList.Display(levels, selectedLevel, selectedDiff);

            List<Chart> charts = selectedLevel.Charts.ToList();
            diffList.Display(charts, selectedDiff);
        }
    } 
}