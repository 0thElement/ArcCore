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
            Display();
        }
        
        private const string lastSelectedPackPref = "LastSelectedPack";
        private const string lastSelectedSongPref = "LastSelectedLevel";
        private const string lastSelectedDiffPref = "LastSelectedDiff";

        [SerializeField] public PackListDisplay packList;
        [SerializeField] public LevelListDisplay levelList;
        [SerializeField] public LevelInfoDisplay levelInfo;
        [SerializeField] public DifficultyListDisplay diffList;

        private Level selectedLevel;
        private Pack selectedPack;
        private DifficultyGroup selectedDiff = new DifficultyGroup { Precedence = 0 };

        public delegate void OnPackChangeDelegate(Pack pack);
        public OnPackChangeDelegate OnPackChange;
        public delegate void OnLevelChangeDelegate(Level level);
        public OnLevelChangeDelegate OnLevelChange;
        public delegate void OnDifficultyChangeDelegate(DifficultyGroup difficultyGroup);
        public OnDifficultyChangeDelegate OnDifficultyChange;

        private List<Pack> packsData => PackQuery.List().ToList();
        private List<Level> levelsData => LevelQuery.List().ToList();

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