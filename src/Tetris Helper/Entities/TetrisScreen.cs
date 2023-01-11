using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

// This feels like bad code etiquette. Ah well.
namespace Celeste.Mod.AnarchyCollab2022 {

    [CustomEntity("AnarchyCollab2022/TetrisScreen")]
    [Tracked]
    //don't exclusively code at 4AM (gone wrong)
    class TetrisScreen : Solid {

        private enum TetrisState {
            Empty,
            I,
            J,
            L,
            O,
            S,
            Z,
            T
        }

        private static List<TetrisState> AllPieces = new List<TetrisState>() {
            TetrisState.I,
            TetrisState.J,
            TetrisState.L,
            TetrisState.O,
            TetrisState.S,
            TetrisState.T,
            TetrisState.Z
        };

        #region Tetris Initialization
        private static Dictionary<TetrisState, int[,]> PieceShapes = new Dictionary<TetrisState, int[,]>() {
            {TetrisState.Empty, new int[,]{
                {1},
            } },
            {TetrisState.I, new int[,]{
                {0, 0, 0, 0},
                {1, 1, 1, 1},
                {0, 0, 0, 0},
                {0, 0, 0, 0}
            } },
            {TetrisState.J, new int[,]{
                {1, 0, 0},
                {1, 1, 1},
                {0, 0, 0}
            } },
            {TetrisState.L, new int[,]{
                {0, 0, 1},
                {1, 1, 1},
                {0, 0, 0}
            } },
            {TetrisState.O, new int[,]{
                {1, 1},
                {1, 1}
            } },
            {TetrisState.S, new int[,]{
                {0, 1, 1},
                {1, 1, 0},
                {0, 0, 0}
            } },
            {TetrisState.Z, new int[,]{
                {1, 1, 0},
                {0, 1, 1},
                {0, 0, 0}
            } },
            {TetrisState.T, new int[,]{
                {0, 1, 0},
                {1, 1, 1},
                {0, 0, 0}
            } }
        };

        #region SRS
        //Idk a better way to do this
        //probably smuggle it away in its own file

        //0 -> spawn state, 1 -> one clockwise rotation, 2 -> two rotations, 3 -> counterclockwise rotation
        //values from https://tetris.fandom.com/wiki/SRS
        //int[] is (current state, attempted rotation state)

        //values are simply reversed if rotated in the opposite direction
        private static Dictionary<Tuple<int, int>, Vector2[]> RegularPieceWallKickData = new Dictionary<Tuple<int, int>, Vector2[]>() {
            {Tuple.Create(0, 1),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(-1, 0),
                new Vector2(-1, 1),
                new Vector2(0, -2),
                new Vector2(-1, -2),
            } },
            {Tuple.Create(1, 2),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, -1),
                new Vector2(0, 2),
                new Vector2(1, 2),
            } },
            {Tuple.Create(2, 3),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, -2),
                new Vector2(1, -2),
            } },
            {Tuple.Create(3, 0),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(-1, 0),
                new Vector2(-1, -1),
                new Vector2(0, 2),
                new Vector2(-1, 2),
            } },
        };

        private static Dictionary<Tuple<int, int>, Vector2[]> IPieceWallKickData = new Dictionary<Tuple<int, int>, Vector2[]>() {
            {Tuple.Create(0, 1),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(-2, 0),
                new Vector2(1, 0),
                new Vector2(-2, -1),
                new Vector2(1, 2),
            } },
            {Tuple.Create(1, 2),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(-1, 0),
                new Vector2(2, 0),
                new Vector2(-1, 2),
                new Vector2(2, -1),
            } },
            {Tuple.Create(2, 3),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(2, 0),
                new Vector2(-1, 0),
                new Vector2(2, 1),
                new Vector2(-1, -2),
            } },
            {Tuple.Create(3, 0),  new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(-2, 0),
                new Vector2(1, -2),
                new Vector2(-2, 1),
            } },
        };

        #endregion

        //colours of pieces
        private Dictionary<TetrisState, Color> PieceColour = new Dictionary<TetrisState, Color>() {
            {TetrisState.Empty, new Color(0f, 0f, 0f, 0f)},
            {TetrisState.I, new Color(0f, 1f, 1f, 1f)},
            {TetrisState.J, new Color(0f, 0f, 1f, 1f)},
            {TetrisState.L, new Color(1f, 0.5f, 0f, 1f)},
            {TetrisState.O, new Color(1f, 1f, 0f, 1f)},
            {TetrisState.S, new Color(0f, 1f, 0f, 1f)},
            {TetrisState.T, new Color(0.5f, 0f, 0.5f, 1f)},
            {TetrisState.Z, new Color(1f, 0f, 0f, 1f)},
        };

        #endregion
        private List<List<TetrisState>> PlayableBoard = new List<List<TetrisState>>();

        class TetrisPiece : Actor {
            public TetrisState State;
            public TetrisScreen Parent;
            public int Rotation;
            private Vector2 origPos;

            public float EditTime;

            public bool[,] BoolThisPieceShape;

            public float timeSinceLastMove;

            public TetrisPiece(Vector2 Position, TetrisState State, TetrisScreen Parent) : base(Position) {
                this.State = State;
                this.Parent = Parent;
                this.Rotation = 0;

                this.origPos = Position;

                timeSinceLastMove = 0;

                int[,] thisShape = PieceShapes[State];
                BoolThisPieceShape = new bool[thisShape.GetLength(0), thisShape.GetLength(1)];

                BoolThisPieceShape = ArrayOperations.IntToBool(thisShape);

                UpdateCollider();
            }

            public override void Added(Scene scene) {
                base.Added(scene);
                if (CollideCheck<TetrisScreen>() && !KillingPlayerFlag) {
                    Scene.Tracker.GetEntity<Player>().Die(Vector2.Zero);
                    KillingPlayerFlag = true;
                }
            }

            public void GoToStartPos(TetrisScreen screen) {
                Position = origPos;
                Rotate(0);
            }

            public void UpdateCollider() {
                //not set to instance of object
                Collider = new Grid(BoolThisPieceShape.GetLength(0), BoolThisPieceShape.GetLength(1), 8, 8);
                (Collider as Grid).Data = new VirtualMap<bool>(BoolThisPieceShape);
            }

            public void Rotate(int state) {
                BoolThisPieceShape = ArrayOperations.IntToBool(PieceShapes[State]);
                switch (state) {
                    case 1:
                        BoolThisPieceShape = ArrayOperations.RotateCW(BoolThisPieceShape);
                        break;
                    case 2:
                        BoolThisPieceShape = ArrayOperations.Rotate180(BoolThisPieceShape);
                        break;
                    case 3:
                        BoolThisPieceShape = ArrayOperations.RotateCCW(BoolThisPieceShape);
                        break;
                    default:
                        break;
                }
                Rotation = state;
            }

            public void RotateDir(int direction) {
                //+4 because I don't know how modulo works
                int oldRotation = Rotation;
                int newRotation = (Rotation + direction + 4) % 4;

                Tuple<int, int> lookup = Tuple.Create(Rotation, newRotation);
                //check if backwards
                if (direction == -1) {
                    lookup = Tuple.Create(newRotation, Rotation);
                }

                //rotate piece, to check collisions
                Rotate(newRotation);
                Vector2 orgPosition = Position;

                Vector2[] offsets;

                try {
                    if (State == TetrisState.I) {
                        offsets = IPieceWallKickData[lookup];

                    } else {
                        offsets = RegularPieceWallKickData[lookup];
                    }

                    if (direction == 1) {
                        offsets = offsets.AsEnumerable().Select(o => o = -o).ToArray();
                    }

                    bool foundRotation = false;
                    int enumerated = 0;
                    foreach (Vector2 offset in offsets) {
                        Position = orgPosition + offset * 8;
                        UpdateCollider();

                        enumerated += 1;

                        //if colliding with any solid tiles, reset
                        if (CollideCheck<TetrisScreen>() || CollideCheck<SolidTiles>()) {
                            continue;
                        }

                        foundRotation = true;
                        Rotation = newRotation;
                        break;
                    }

                    if (!foundRotation) {
                        //if all fail, don't
                        Position = orgPosition;
                        Rotate(oldRotation);
                        UpdateCollider();
                    }
                } catch (KeyNotFoundException) { }
            }

            public Vector2 GetHardDropPosition() {
                Vector2 orgPosition = Position;
                MoveV(1000);
                Vector2 newPosition = Position;
                Position = orgPosition;
                return newPosition;
            }

            public override void Update() {
                base.Update();

                if (Scene.Tracker.GetEntity<TetrisScreen>().Finished) {
                    return;
                }

                timeSinceLastMove += Engine.DeltaTime;

                if (timeSinceLastMove > 1) {
                    MoveV(8, c => LandPiece());
                    timeSinceLastMove = 0;
                }
            }

            private void LandPiece() {
                Parent.AddPiece((Collider as Grid).Data, State);
                Parent.RequestPiece(false);
            }

            public override void Render() {
                foreach (TileGrid tg in Components.ToArray()) {
                    Remove(tg);
                }

                //render active piece
                TileGrid currentPieceTiles = TileMapFromPiece((Collider as Grid).Data, State);
                currentPieceTiles.Position = new Vector2(0, 0);
                Add(currentPieceTiles);

                //render piece shadow
                //don't if tracker not set up yet
                TileGrid shadowPieceTiles = TileMapFromPiece((Collider as Grid).Data, State);
                shadowPieceTiles.Position = GetHardDropPosition() - Position;
                shadowPieceTiles.Alpha = 0.3f;
                Add(shadowPieceTiles);

                //debug render
                #region debugrender
                bool debugrenderthis = false;
                if (debugrenderthis) {
                    Vector2 orgPosition = Position;

                    Vector2[] offsets;

                    int direction = 1;
                    int oldRotation = Rotation;
                    int newRotation = (Rotation + direction + 4) % 4;
                    Rotate(newRotation);
                    Grid rotatedPiece = new Grid(BoolThisPieceShape.GetLength(0), BoolThisPieceShape.GetLength(1), 8, 8);
                    rotatedPiece.Data = new VirtualMap<bool>(BoolThisPieceShape);
                    Rotate(oldRotation);

                    Tuple<int, int> lookup = Tuple.Create(oldRotation, newRotation);
                    //check if backwards
                    if (direction == -1) {
                        lookup = Tuple.Create(newRotation, oldRotation);
                    }

                    try {
                        if (State == TetrisState.I) {
                            offsets = IPieceWallKickData[lookup];

                        } else {
                            offsets = RegularPieceWallKickData[lookup];
                        }
                        int enumerate = 0;
                        if (direction == 1) {
                            offsets = offsets.AsEnumerable().Select(o => o = -o).ToArray();
                        }
                        foreach (Vector2 offset in offsets) {
                            rotatedPiece.Position = orgPosition + offset * 8;
                            enumerate++;
                            rotatedPiece.Render((Scene as Level).Camera, new Color(255, 255, enumerate * 50));
                        }
                    } catch (KeyNotFoundException) { }
                }
                #endregion

                base.Render();
            }

            public override void DebugRender(Camera camera) {
                base.DebugRender(camera);
                //collider is rotated the complete wrong way, most likely due to debug
                (Collider as Grid).Render(camera, Color.Red);
            }
        }

        public int LineCount;
        public int TotalLineCount;
        public float Timer;
        public float TotalTime;
        public bool Finished;

        private TetrisPiece CurrentPiece;

        private TetrisState HoldPiece;

        private TetrisState[] PieceQueue = new TetrisState[5];
        private List<TetrisState> NextPieces = new List<TetrisState>();

        private static Dictionary<TetrisState, Char> Tilesets;

        private CoreMessage timer;
        private CoreMessage lineClears;

        private DynData<CoreMessage> timerData;
        private DynData<CoreMessage> lineClearsData;

        public TetrisScreen(EntityData data, Vector2 offset, EntityID gid) : base(data.Position + offset, 80, 160, true) {
            Logger.Log("AnarchyCodeMod", "Loaded Tetris Screen");
            //initialize piece shapes
            Tilesets = new Dictionary<TetrisState, char>(){
                { TetrisState.Empty,'0'},
                { TetrisState.I, data.Char("ITiles", '1')},
                { TetrisState.J, data.Char("JTiles", '3')},
                { TetrisState.L, data.Char("LTiles", '4')},
                { TetrisState.O, data.Char("OTiles", '5')},
                { TetrisState.S, data.Char("STiles", '6')},
                { TetrisState.T, data.Char("TTiles", '7')},
                { TetrisState.Z, data.Char("ZTiles", '8')}
            };

            /*
            Tilesets = new Dictionary<TetrisState, char>(){
                { TetrisState.Empty,'0'},
                { TetrisState.I, 'Ⓘ'},
                { TetrisState.J, 'Ⓙ'},
                { TetrisState.L, 'Ⓛ'},
                { TetrisState.O, 'Ⓞ'},
                { TetrisState.S, 'Ⓢ'},
                { TetrisState.T, 'Ⓣ'},
                { TetrisState.Z, 'Ⓩ'}
            };
            */

            EntityData nullData = new EntityData();
            nullData.Position = Vector2.Zero;
            timer = new CoreMessage(nullData, Position + new Vector2(-52, 0));
            timerData = new DynData<CoreMessage>(timer);
            lineClears = new CoreMessage(nullData, Position + new Vector2(-52, -20));
            lineClearsData = new DynData<CoreMessage>(lineClears);

            Timer = 0;
            TotalTime = data.Float("Time", 120);
            LineCount = 0;
            TotalLineCount = data.Int("LineCount", 40);
            Finished = false;

            InitInputs();

            

            Collider = new Grid(10, 20, 8, 8);
            Collider.Position = new Vector2(-40, -80);

            for (int i = 0; i < 20; i++) {
                PlayableBoard.Add(new List<TetrisState>(new TetrisState[10]));
            }

        }

        private void GenerateBag() {
            // Change Randomizer
            Calc.PushRandom((int)(SceneAs<Level>().Session.Time % int.MaxValue));
            
            foreach (TetrisState piece in AllPieces.OrderBy(a => Calc.Random.NextDouble()).ToList()) {
                NextPieces.Add(piece);
            }

            // Reset Randomizer
            Calc.PopRandom();
        }

        public static void Load() {
            On.Celeste.Settings.Reload += Settings_Reload;
            On.Celeste.Switch.Check += Switch_Check;
            On.Monocle.Grid.Collide_Grid += Grid_Collide_Grid;
        }

        //whoops
        private static bool Grid_Collide_Grid(On.Monocle.Grid.orig_Collide_Grid orig, Grid self, Grid grid) {
            try {
                return orig(self, grid);
            } catch (NotImplementedException) {
                return UtilityMethods.CollideCheck(self, grid);
            }

        }

        public static void Unload() {
            On.Celeste.Settings.Reload -= Settings_Reload;
            On.Celeste.Switch.Check -= Switch_Check;
            On.Monocle.Grid.Collide_Grid -= Grid_Collide_Grid;
        }

        private static void Settings_Reload(On.Celeste.Settings.orig_Reload orig) {
            orig();
            InitInputs();
        }

        public static void InitInputs() {
            AnarchyCollabSettings settings = AnarchyCollabModule.Settings;
            //first repeat time (DAS), second repeat time (ARR)
            //should be called whenever settings are changed
            //this can mess things up
            //settings.LeftButton.BufferTime = 0;
            settings.LeftButton.SetRepeat(settings.DAS / 60, settings.ARR / 60);
            //settings.RightButton.BufferTime = 0;
            settings.RightButton.SetRepeat(settings.DAS / 60, settings.ARR / 60);

            //settings.SoftDropButton.BufferTime = 0;
            //default of 10, or 6 inputs per second
            settings.SoftDropButton.SetRepeat(10 / settings.SDF, 10 / settings.SDF);

            //settings.HardDropButton.BufferTime = 0;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            GenerateBag();

            scene.Add(timer);
            scene.Add(lineClears);
        }

        static bool KillingPlayerFlag = false;

        private static bool Switch_Check(On.Celeste.Switch.orig_Check orig, Scene scene) {
            if (scene.Tracker.GetEntity<TetrisScreen>() != null) {
                TetrisScreen t = scene.Tracker.GetEntity<TetrisScreen>();
                return t.Finished;
            } else {
                return orig(scene);
            }
        }

        public override void Update() {
            base.Update();

            if (Finished) {
                return;
            }

            if (CurrentPiece == null) {
                RequestPiece(false);
            }

            float deltaTime = Engine.DeltaTime;
            if (LineCount >= TotalLineCount) {
                //subtract, so that time doesn't change
                Timer -= deltaTime;
                Finished = true;
                //Scene.Tracker.GetComponent<Switch>()?.Finished = true;
            }

            Timer += deltaTime;

            //check death
            if (Timer > TotalTime && !KillingPlayerFlag) {
                Scene.Tracker.GetEntity<Player>().Die(Vector2.Zero);
                KillingPlayerFlag = true;
                //(Scene as Level).Reload();
            }

            //check if any lines are full
            foreach (List<TetrisState> row in PlayableBoard.ToArray()) {
                if (!row.Contains(TetrisState.Empty)) {
                    PlayableBoard.Remove(row);
                    PlayableBoard.Insert(0, new List<TetrisState>(new TetrisState[10]));
                    LineCount += 1;
                }
            }

            bool[,] colliderData = new bool[10, 20];
            //this loop again :/

            for (int k = 0; k < colliderData.GetLength(0); k++) {
                for (int l = 0; l < colliderData.GetLength(1); l++) {
                    colliderData[k, l] = PlayableBoard[l][k] != TetrisState.Empty;
                }
            }
            //true if there is a tile there
            (Collider as Grid).Data = new VirtualMap<bool>(colliderData);

            CurrentPiece.UpdateCollider();

            //handle inputs
            AnarchyCollabSettings settings = AnarchyCollabModule.Settings;
            //left
            try {
                bool af1 = true;
                if (settings.LeftButton.Pressed) {
                    settings.LeftButton.ConsumePress();
                    CurrentPiece.MoveH(-8, c => af1 = false);
                    if (af1) { Audio.Play("event:/billybobbyjoeyIguess/click5"); }
                }
                //right
                bool af2 = true;
                if (settings.RightButton.Pressed) {
                    settings.RightButton.ConsumePress();
                    CurrentPiece.MoveH(8, c => af2 = false);
                    if (af2) { Audio.Play("event:/billybobbyjoeyIguess/click5"); }
                }
                //softdrop
                bool af3 = true;
                if (settings.SoftDropButton.Pressed) {
                    settings.SoftDropButton.ConsumePress();
                    //TODO: move more than one
                    if (settings.SDF > 999) {
                        CurrentPiece.MoveV(9999, c => af3 = false);
                    } else {
                        CurrentPiece.MoveV(8, c => af3 = false);
                    }
                    if (af3) { Audio.Play("event:/billybobbyjoeyIguess/click5"); }
                }
                //harddrop
                if (settings.HardDropButton.Pressed) {
                    settings.HardDropButton.ConsumePress();
                    CurrentPiece.MoveV(1000);
                    CurrentPiece.timeSinceLastMove = 9999;
                    Audio.Play("event:/billybobbyjoeyIguess/click2");
                }
                //rotate left
                if (settings.RotateCWButton.Pressed) {
                    settings.RotateCWButton.ConsumePress();
                    CurrentPiece.RotateDir(-1);
                    Audio.Play("event:/billybobbyjoeyIguess/bleep1");
                }
                //rotate right
                if (settings.RotateCCWButton.Pressed) {
                    settings.RotateCCWButton.ConsumePress();
                    CurrentPiece.RotateDir(1);
                    Audio.Play("event:/billybobbyjoeyIguess/bleep2");
                }
                //rotate 180
                if (settings.Rotate180Button.Pressed) {
                    settings.Rotate180Button.ConsumePress();
                    //just a double rotation
                    CurrentPiece.RotateDir(1);
                    CurrentPiece.RotateDir(1);
                    Audio.Play("event:/billybobbyjoeyIguess/bleep1");
                }
                //hold
                if (settings.HoldButton.Pressed) {
                    settings.HoldButton.ConsumePress();

                    RequestPiece(true);
                }
            } catch (NullReferenceException e) {
                Logger.Log("Tetris Helper", $"{e}, {e.StackTrace}");
            }
        }

        private void RequestPiece(bool hold) {
            //remove piece
            Scene.Remove(CurrentPiece);

            TetrisState PieceState = NextPieces.First();
            //CurrentPiece = new TetrisPiece(Position + new Vector2(0, -80), NextPieces.First(), this);
            if (hold) {
                if (HoldPiece == TetrisState.Empty) {
                    NextPieces.RemoveAt(0);

                } else {
                    PieceState = HoldPiece;
                    //don't need to remove from queue
                }
                HoldPiece = CurrentPiece.State;

            } else {
                NextPieces.RemoveAt(0);
            }

            Vector2 PiecePosition = Position + new Vector2(0, -80);
            CurrentPiece = new TetrisPiece(PiecePosition, PieceState, this);

            Scene.Add(CurrentPiece);

            //get all pieces
            //hardcoded because lame
            if (NextPieces.Count < 5) {
                GenerateBag();
            }
            PieceQueue = NextPieces.GetRange(0, 5).ToArray();
        }

        private void AddPiece(VirtualMap<bool> data, TetrisState state) {
            for (int k = 0; k < data.Columns; k++) {
                for (int l = 0; l < data.Rows; l++) {
                    //reverse
                    Vector2 pieceLocation = (CurrentPiece.Position - (Position + new Vector2(-40, -80))) / 8;
                    if (data[l, k]) {
                        try {
                            PlayableBoard[k + (int)pieceLocation.Y][l + (int)pieceLocation.X] = state;
                        } catch (IndexOutOfRangeException) { }
                    }
                }
            }
        }

        public override void Render() {
            //render text
            //update text
            timerData["alpha"] = 1;
            timerData["text"] = $"{Math.Round(Timer, 1)} / {TotalTime}";
            //TODO: change based on input
            lineClearsData["alpha"] = 1;
            lineClearsData["text"] = $"{LineCount} / {TotalLineCount}";

            timer.Render();
            lineClears.Render();

            //instead of doing this, have all the tiles maybe just be constant
            //remove all previous tiles
            foreach (TileGrid t in Components.GetAll<TileGrid>().ToList()) {
                Remove(t);
            }

            //render board
            VirtualMap<char> virtualMap = new VirtualMap<char>(10, 20, '0');

            for (int k = 0; k < virtualMap.Rows; k++) {
                for (int l = 0; l < virtualMap.Columns; l++) {
                    //might be reversed
                    virtualMap[l, k] = Tilesets[PlayableBoard[k][l]];
                }
            }

            TileGrid tiles = GFX.FGAutotiler.GenerateMap(virtualMap, new Autotiler.Behaviour {
                EdgesExtend = false,
                EdgesIgnoreOutOfLevel = false,
                PaddingIgnoreOutOfLevel = false
            }).TileGrid;

            tiles.Position = new Vector2(-40, -80);

            Add(tiles);

            //render piece queue
            //top is 184, 24
            for (int i = 0; i < PieceQueue.Length; i++) {
                TileGrid thisPieceTiles = TileMapFromPiece(PieceQueue[i]);
                thisPieceTiles.Position = new Vector2(56, i * 24 - 72) + GetSpecificPieceAdjustment(PieceQueue[i]);
                Add(thisPieceTiles);

            }

            //render held piece
            //48, 24
            TileGrid heldPieceTiles = TileMapFromPiece(HoldPiece);
            heldPieceTiles.Position = new Vector2(-80, -72) + GetSpecificPieceAdjustment(HoldPiece);
            Add(heldPieceTiles);

            base.Render();
        }

        public override void DebugRender(Camera camera) {
            base.DebugRender(camera);

            Collider.Render(camera, Color.Blue);
        }

        //I and O pieces need to be drawn differently while in menu, so that they are centered
        private static Vector2 GetSpecificPieceAdjustment(TetrisState piece) {
            Vector2 offset = Vector2.Zero;
            if (piece == TetrisState.I) {
                offset += new Vector2(-4, -4);
            }
            if (piece == TetrisState.O) {
                offset += new Vector2(4, 0);
            }
            return offset;
        }

        private static TileGrid TileMapFromPiece(TetrisState piece) {
            int[,] thisPieceShape = PieceShapes[piece];

            VirtualMap<char> pieceTileMap = new VirtualMap<char>(thisPieceShape.GetLength(0), thisPieceShape.GetLength(1), (char)TetrisState.Empty);
            for (int k = 0; k < thisPieceShape.GetLength(0); k++) {
                for (int l = 0; l < thisPieceShape.GetLength(1); l++) {
                    //key not found
                    TetrisState thisPieceState = thisPieceShape[l, k] == 0 ? TetrisState.Empty : piece;
                    //reverse
                    pieceTileMap[k, l] = Tilesets[thisPieceState];
                }
            }

            return TileMapFromPiece(pieceTileMap);
        }



        private static TileGrid TileMapFromPiece(VirtualMap<bool> data, TetrisState state) {
            VirtualMap<char> pieceTileMap = new VirtualMap<char>(data.Columns, data.Rows, (char)TetrisState.Empty);
            for (int k = 0; k < data.Columns; k++) {
                for (int l = 0; l < data.Rows; l++) {
                    pieceTileMap[l, k] = data[l, k] ? Tilesets[state] : Tilesets[TetrisState.Empty];
                }
            }
            return TileMapFromPiece(pieceTileMap);
        }

        private static TileGrid TileMapFromPiece(VirtualMap<char> pieceTileMap) {
            TileGrid tiles = GFX.FGAutotiler.GenerateMap(pieceTileMap, new Autotiler.Behaviour {
                EdgesExtend = false,
                EdgesIgnoreOutOfLevel = false,
                PaddingIgnoreOutOfLevel = false
            }).TileGrid;

            return tiles;
        }
    }
}
