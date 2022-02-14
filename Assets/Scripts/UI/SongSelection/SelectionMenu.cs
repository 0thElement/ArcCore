using UnityEngine;
using ArcCore.Storage.Data;
using System.Collections.Generic;
using System.Linq;
using ArcCore.Scenes;
using ArcCore.Gameplay;

namespace ArcCore.UI.SongSelection
{
    public class SelectionMenu : MonoBehaviour
    {
        public static SelectionMenu Instance;
        protected void Awake()
        {
            Instance = this;
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
        private DifficultyGroup selectedDiff;

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
                        if (!diffIncluded)
                        {
                            selectedDiff = value.GetClosestChart(selectedDiff).DifficultyGroup;
                            DrawLevels();
                        }
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
                    DrawDifficulties();
                    DrawLevels();
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

            if (object.ReferenceEquals(selectedDiff, null))
            {
                int min = int.MaxValue;
                DifficultyGroup closestDiff = null;
                DifficultyGroup dummyGroup = new DifficultyGroup { Precedence = 0 };

                foreach (Level level in levels)
                {
                    DifficultyGroup diff = level.GetClosestChart(dummyGroup).DifficultyGroup;
                    if (diff.Precedence < min)
                    {
                        min = diff.Precedence;
                        closestDiff = diff;
                    }
                }
                selectedDiff = closestDiff;
            }

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

        public void SelectChart(Level level, Chart chart)
        {
            SceneTransitionManager.Instance.SwitchToPlayScene(level, chart,
                (playResult) => {
                    //Return back to this scene after result screen
                    //No use for playResult here
                    SceneTransitionManager.Instance.SwitchScene(SceneNames.selectionScene);
                },
                (sceneRepresentative) => {
                    PlaySceneRepresentative playScene = sceneRepresentative as PlaySceneRepresentative;
                    playScene.LoadChart(level, chart);
                }
            );
        }
    } 
}