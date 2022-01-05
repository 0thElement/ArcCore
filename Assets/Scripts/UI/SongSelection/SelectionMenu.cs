using UnityEngine;
using UnityEngine.UI;
using ArcCore.Serialization;
using System.Collections.Generic;

namespace ArcCore.UI.SongSelection
{
    public class SelectionMenu : ScrollRect
    {
        public static MainMenu Instance;
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            GetData();
        }
        
        private static const string lastSelectedPackPref = "LastSelectedPack";
        private static const string lastSelectedSongPref = "LastSelectedLevel";
        private static const string lastSelectedDiffPref = "LastSelectedDiff";

        [SerializeField] public LevelListDisplay levelList;
        [SerializeField] public PackListDisplay packList;
        [SerializeField] public DifficultyListDisplay diffList;

        [HideFromInspector] public List<Level> levelsData;
        [HideFromInspector] public List<Pack> packsData;

        private Level selectedLevel;
        private Pack selectedPack;
        private Difficulty selectedDiff;

        public Pack selectedPack
        {
            get => selectedPack;
            set {
                selectedPack = value;
                PlayerPrefs.SetString(lastSelectedPackPref, value);
                Draw();
            }
        }

        public Level SelectedLevel
        {
            get => selectedLevel;
            set {
                selectedLevel = value;
                PlayerPrefs.SetString(lastSelectedLevelPref, value);
                selectedLevel.GetClosestChart(SelectedDiff);
                Draw();
            }
        }

        public Difficulty SelectedDiff
        {
            get => selectedDiff;
            set {
                selectedDiff = value;
                PlayerPrefs.SetString(lastSelectedDiffPref, value);
                Draw();
            }
        }

        private void GetData()
        {
            Difficulty pst = new Difficulty() {
                Color = new Color(),
                Name = "Past",
                LevelName = "PST",
                IsPlus = true,
                Precedence = 0
            };
            Difficulty prs = new Difficulty() {
                Color = new Color(),
                Name = "Present",
                LevelName = "PRS",
                IsPlus = true,
                Precedence = 10
            };
            Difficulty ftr = new Difficulty() {
                Color = new Color(),
                Name = "Future",
                LevelName = "FTR",
                IsPlus = true,
                Precedence = 20
            };
            Difficulty byd = new Difficulty() {
                Color = new Color(),
                Name = "Beyond",
                LevelName = "BYD",
                IsPlus = true,
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
                            Difficulty = pst,
                            SongPath = "/data/test1/base.ogg",
                            ImagePath = "/data/test1/base.jpg",
                            Name = "Song1",
                            NameRomanized = "Song1",
                            Artist = "Artist1",
                            ArtistRomanized = "Artist1",
                            Bpm = 100,
                            Constant = 4,
                            PbScore = 10000255,
                            PbGrade = ScoreCategory.PureMemory,
                            Settings = defaultSettings,
                            Charter = "Charter1"
                        },
                        new Chart() {
                            Difficulty = prs,
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
                            Difficulty = ftr,
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
                            Difficulty = byd,
                            SongPath = "/data/test1/remix.ogg",
                            ImagePath = "/data/test1/remix.jpg",
                            Name = "Song1 remix",
                            NameRomanized = "Song1 remix",
                            Artist = "Artist1 but cooler",
                            ArtistRomanized = "Artist1 but cooler",
                            Bpm = "150",
                            Constant = 10.8,
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
                            Difficulty = pst,
                            SongPath = "/data/test2/base.ogg",
                            ImagePath = "/data/test2/base.jpg",
                            Name = "Song2",
                            NameRomanized = "Song2",
                            Artist = "Artist2",
                            ArtistRomanized = "Artist2",
                            Bpm = 100,
                            Constant = 4,
                            PbScore = 10000255,
                            PbGrade = ScoreCategory.PureMemory,
                            Settings = defaultSettings,
                            Charter = "Charter5"
                        },
                        new Chart() {
                            Difficulty = prs,
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
                            Difficulty = ftr,
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
                            Difficulty = byd,
                            SongPath = "/data/test1/remix.ogg",
                            ImagePath = "/data/test1/remix.jpg",
                            Name = "Song2 remix",
                            NameRomanized = "Song2 remix",
                            Artist = "Artist2 but cooler",
                            ArtistRomanized = "Artist2 but cooler",
                            Bpm = "150",
                            Constant = 10.8,
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
            //TODO: FIND A BETTER WAY TO STORE THIS SHIT AND STORE SELECTED LEVEL PER PACK
            selectedLevel = PlayerPrefs.GetString(lastSelectedLevelPref);
            selectedPack = PlayerPrefs.GetString(lastSelectedPackPref);
            selectedDiff = PlayerPrefs.GetString(lastSelectedDiffPref);
            Draw();
        }

        private void Draw()
        {
            packList.Display(packs, selectedPack);

            List<Level> levels = levelsData;
            if (selectedLevel != null) {
                levels = levelsData.Where(level => level.Pack != null && level.Pack.PackPath == selectedPack.PackPath);
            }
            levelList.Display(levels, selectedLevel, selectedDiff);

            List<Chart> charts = selectedLevel.Charts;
            diffList.Display(charts, selectedDiff);
        }
    } 
}