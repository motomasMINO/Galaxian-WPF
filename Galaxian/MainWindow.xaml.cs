using System.ComponentModel;
using System.IO;
using System.Media;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;

namespace Galaxian
{
    public partial class MainWindow : Window
    {
        public class Block
        {
            public double X;
            public double Y;
            public double Width;
            public double Height;
            public ImageSource Img;
            public bool Alive = true;
            public bool Used = false;

            public Block(double x, double y, double width, double height, ImageSource img)
            {
                X = x; 
                Y = y; 
                Width = width; 
                Height = height; 
                Img = img;
            }
        }

        // ボード設定
        private const int TileSize = 32;
        private const int Rows = 16;
        private const int Columns = 16;
        private const int BoardWidth = TileSize * Columns;   // 512
        private const int BoardHeight = TileSize * Rows;     // 512

        // 自機
        private double ShipWidth = TileSize * 2;
        private double ShipHeight = TileSize;
        private double ShipStartX = TileSize * Columns / 2 - TileSize;
        private double ShipStartY = BoardHeight - TileSize * 2;
        private double ShipVelocityX = TileSize;
        private Block Ship;

        // 旗艦
        private List<Block> FragshipArray = new();
        private double FragshipWidth = TileSize * 2;
        private double FragshipHeight = TileSize;
        private double FragshipX = TileSize;
        private double FragshipY = TileSize;
        private double FragshipVelocityX = 1;

        // エイリアン
        private List<Block> AlienRedArray = new();
        private List<Block> AlienPinkArray = new();
        private List<Block> AlienCyanArray = new();
        private double AlienRedX = TileSize;
        private double AlienRedY = TileSize * 2;
        private double AlienPinkY = TileSize * 3;
        private double AlienCyanY = TileSize * 4;
        private double AlienVelocityX = 1;

        // 弾丸
        private List<Block> BulletArray = new();
        private double BulletWidth = TileSize / 8.0;
        private double BulletHeight = TileSize / 2.0;
        private double BulletVelocityY = -15;

        // 敵弾
        private List<Block> EnemyBulletArray = new();
        private double EnemyBulletWidth = TileSize / 8.0;
        private double EnemyBulletHeight = TileSize / 2.0;
        private double EnemyBulletVelocityY = 8;

        private DispatcherTimer GameLoop;
        private int Score = 0;
        private int Lives = 3;
        private int Rounds = 1;
        private int NextLifeScore = 10000;
        private bool GameOver = false;

        // 画像
        private ImageSource ShipImg;
        private ImageSource FragshipImg;
        private ImageSource AlienRedImg;
        private ImageSource AlienPinkImg;
        private ImageSource AlienCyanImg;

        // サウンド
        private Sound GameStart;
        private Sound BackgroundSound;
        private SoundEffect ShootSound;
        private SoundEffect ExplosionSound;
        private SoundEffect BossExplosionSound;
        private SoundEffect Loss;
        private SoundEffect ExtraLife;

        private Random rand = new();

        // ハイスコア保存先
        private readonly string HighScoreFile;

        public MainWindow()
        {
            InitializeComponent();

            // Canvas サイズを明示
            GameCanvas.Width = BoardWidth;
            GameCanvas.Height = BoardHeight;

            // フォーカス確保
            this.Loaded += (s, e) => { GameCanvas.Focus(); };

            // タイマー
            GameLoop = new DispatcherTimer(DispatcherPriority.Render);
            GameLoop.Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0); // 60fps
            GameLoop.Tick += (s, e) => 
            { 
                Move(); 
                Render(); 
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 画像を読み込む
            ShipImg = LoadImage("Resources/Galacship.png");
            FragshipImg = LoadImage("Resources/Fragship.png");
            AlienRedImg = LoadImage("Resources/RedAlien.jpg");
            AlienPinkImg = LoadImage("Resources/PinkAlien.jpg");
            AlienCyanImg = LoadImage("Resources/CyanAlien.jpg");

            // サウンド読み込み（簡易）
            TryLoadSoundPlayers();

            // 初期化
            Ship = new Block(ShipStartX, ShipStartY, ShipWidth, ShipHeight, ShipImg);
            CreateFragship();
            CreateRedAliens();
            CreatePinkAliens();
            CreateCyanAliens();

            // スタート
            GameLoop.Start();
            GameStart?.Play();
            BackgroundSound.Loop();
        }

        private ImageSource LoadImage(string relativePath)
        {
            try
            {
                var uri = new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath), UriKind.Absolute);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch
            {
                return null!;
            }
        }

        private void TryLoadSoundPlayers()
        {
            // サウンドを読み込む
            GameStart = new Sound("Resources/StartGame.wav");
            BackgroundSound = new Sound("Resources/backgroundMusic.wav");
            ShootSound = new SoundEffect("Resources/Shoot.wav");
            ExplosionSound = new SoundEffect("Resources/HitEnemy.wav");
            BossExplosionSound = new SoundEffect("Resources/HitBoss.wav");
            Loss = new SoundEffect("Resources/FighterLoss.wav");
            ExtraLife = new SoundEffect("Resources/Extra-Life.wav");
        }

        private void CreateFragship()
        {
            FragshipArray.Clear();
            for (int c = 0; c < 5; c++)
            {
                FragshipArray.Add(new Block(FragshipX + c * FragshipWidth, FragshipY, FragshipWidth, FragshipHeight, FragshipImg));
            }
        }

        private void CreateRedAliens()
        {
            AlienRedArray.Clear();
            for (int c = 0; c < 5; c++)
            {
                AlienRedArray.Add(new Block(AlienRedX + c * FragshipWidth, AlienRedY, FragshipWidth, FragshipHeight, AlienRedImg));
            }
        }

        private void CreatePinkAliens()
        {
            AlienPinkArray.Clear();
            for (int c = 0; c < 5; c++)
            {
                AlienPinkArray.Add(new Block(AlienRedX + c * FragshipWidth, AlienPinkY, FragshipWidth, FragshipHeight, AlienPinkImg));
            }
        }

        private void CreateCyanAliens()
        {
            AlienCyanArray.Clear();
            for (int c = 0; c < 5; c++)
            {
                AlienCyanArray.Add(new Block(AlienRedX + c * FragshipWidth, AlienCyanY, FragshipWidth, FragshipHeight, AlienCyanImg));
            }
        }

        private void Move()
        {
            if (GameOver) return;

            // 自機弾の移動と当たり判定
            for (int i = BulletArray.Count - 1; i >= 0; i--)
            {
                var bullet = BulletArray[i];
                bullet.Y += BulletVelocityY;

                // 旗艦
                foreach (var alien in FragshipArray)
                {
                    if (alien.Alive && DetectCollision(bullet, alien))
                    {
                        alien.Alive = false; 
                        bullet.Used = true;
                        Score += 60; 
                        BossExplosionSound?.Play(); 
                        CheckExtraLife();
                    }
                }
                foreach (var alien in AlienRedArray)
                {
                    if (alien.Alive && DetectCollision(bullet, alien))
                    { 
                        alien.Alive = false; 
                        bullet.Used = true; 
                        Score += 50; 
                        ExplosionSound?.Play(); 
                        CheckExtraLife(); 
                    }
                }
                foreach (var alien in AlienPinkArray)
                {
                    if (alien.Alive && DetectCollision(bullet, alien))
                    { 
                        alien.Alive = false; 
                        bullet.Used = true; 
                        Score += 40; 
                        ExplosionSound?.Play(); 
                        CheckExtraLife(); 
                    }
                }
                foreach (var alien in AlienCyanArray)
                {
                    if (alien.Alive && DetectCollision(bullet, alien))
                    { 
                        alien.Alive = false; 
                        bullet.Used = true; 
                        Score += 30; 
                        ExplosionSound?.Play(); 
                        CheckExtraLife(); 
                    }
                }

                if (bullet.Y < 0 || bullet.Used) BulletArray.RemoveAt(i);
            }

            // 敵の横移動（各行同じ速度）
            UpdateAlienRow(FragshipArray, ref FragshipVelocityX);
            UpdateAlienRow(AlienRedArray, ref AlienVelocityX);
            UpdateAlienRow(AlienPinkArray, ref AlienVelocityX);
            UpdateAlienRow(AlienCyanArray, ref AlienVelocityX);

            // 敵弾の移動（自機ヒット判定）
            for (int i = EnemyBulletArray.Count - 1; i >= 0; i--)
            {
                // 先に存在確認
                if (i < 0 || i >= EnemyBulletArray.Count) break;

                var eb = EnemyBulletArray[i];
                eb.Y += EnemyBulletVelocityY;

                if (DetectCollision(eb, Ship))
                {
                    // 衝突時は弾を全部消して自機をリセット
                    Lives--;
                    EnemyBulletArray.Clear();
                    BulletArray.Clear();
                    Ship.X = ShipStartX;
                    Loss?.Play();

                    // 残機が0ならゲームオーバー
                    if (Lives <= 0)
                    {
                        GameOver = true;
                        GameLoop.Stop();
                        BackgroundSound?.Stop();
                        BulletArray.Clear();
                        EnemyBulletArray.Clear();
                        Ship.Y = 575;
                        GameOverText.Visibility = Visibility.Visible;
                    }
                    break;
                }

                // 画面外に出たら消す
                if(eb.Y > BoardHeight)
                {
                    EnemyBulletArray.RemoveAt(i);
                }
            }

            // 全敵が倒れたかどうか
            if (AreAllEnemiesDefeated())
            {
                BulletArray.Clear();
                EnemyBulletArray.Clear();
                CreateFragship();
                CreateRedAliens();
                CreatePinkAliens();
                CreateCyanAliens();
                Rounds++;
            }

            // 敵がランダムで撃つ
            FireEnemyBullets(FragshipArray);
            FireEnemyBullets(AlienRedArray);
            FireEnemyBullets(AlienPinkArray);
            FireEnemyBullets(AlienCyanArray);

            // HUD 更新
            ScoreText.Text = $"SCORE: {Score}";
            LivesText.Text = $"LIVES: {Lives}";
            RoundText.Text = $"ROUND {Rounds}";
        }

        private void UpdateAlienRow(List<Block> row, ref double velocityX)
        {
            foreach (var alien in row)
            {
                if (!alien.Alive) continue;
                alien.X += velocityX;
                if (alien.X + alien.Width >= BoardWidth || alien.X <= 0)
                {
                    velocityX *= -1;
                    alien.X += velocityX * 2;
                }
            }
        }

        private void CheckExtraLife()
        {
            if (Score >= NextLifeScore)
            {
                Lives++;
                ExtraLife?.Play();
                NextLifeScore += 10000;　// 1万点で1UP
            }
        }

        private void FireEnemyBullets(List<Block> enemies)
        {
            foreach (var alien in enemies)
            {
                if (alien.Alive && rand.Next(0, 200) == 0)
                {
                    EnemyBulletArray.Add(new Block(alien.X + alien.Width / 2 - EnemyBulletWidth / 2,
                                                   alien.Y + alien.Height, EnemyBulletWidth, EnemyBulletHeight, null));
                }
            }
        }

        private bool AreAllEnemiesDefeated()
        {
            return FragshipArray.All(a => !a.Alive)
                && AlienRedArray.All(a => !a.Alive)
                && AlienPinkArray.All(a => !a.Alive)
                && AlienCyanArray.All(a => !a.Alive);
        }

        private bool DetectCollision(Block a, Block b)
        {
            return a != null && b != null &&
                   a.X < b.X + b.Width &&
                   a.X + a.Width > b.X &&
                   a.Y < b.Y + b.Height &&
                   a.Y + a.Height > b.Y;
        }

        private void Render()
        {
            // 描画はUIスレッドで行う（DispatcherTimerがUIスレッド）
            GameCanvas.Children.Clear();

            // 自機描画
            if (Ship.Img != null)
            {
                var img = new Image { Source = Ship.Img, Width = Ship.Width, Height = Ship.Height };
                Canvas.SetLeft(img, Ship.X);
                Canvas.SetTop(img, Ship.Y);
                GameCanvas.Children.Add(img);
            }

            // 敵描画（各種）
            void DrawEnemies(IEnumerable<Block> list)
            {
                foreach (var alien in list)
                {
                    if (!alien.Alive) continue;
                    if (alien.Img != null)
                    {
                        var ai = new Image { Source = alien.Img, Width = alien.Width, Height = alien.Height };
                        Canvas.SetLeft(ai, alien.X);
                        Canvas.SetTop(ai, alien.Y);
                        GameCanvas.Children.Add(ai);
                    }
                    else
                    {
                        var rect = new Rectangle { Width = alien.Width, Height = alien.Height, Fill = Brushes.Gray };
                        Canvas.SetLeft(rect, alien.X);
                        Canvas.SetTop(rect, alien.Y);
                        GameCanvas.Children.Add(rect);
                    }
                }
            }

            DrawEnemies(FragshipArray);
            DrawEnemies(AlienRedArray);
            DrawEnemies(AlienPinkArray);
            DrawEnemies(AlienCyanArray);

            // 自機弾
            foreach (var b in BulletArray)
            {
                var r = new Rectangle { Width = b.Width, Height = b.Height, Fill = Brushes.Yellow };
                Canvas.SetLeft(r, b.X);
                Canvas.SetTop(r, b.Y);
                GameCanvas.Children.Add(r);
            }

            // 敵弾
            foreach (var eb in EnemyBulletArray)
            {
                var r = new Rectangle { Width = eb.Width, Height = eb.Height, Fill = Brushes.White };
                Canvas.SetLeft(r, eb.X);
                Canvas.SetTop(r, eb.Y);
                GameCanvas.Children.Add(r);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // ゲームオーバー時はスペースでリセット
            if (GameOver)
            {
                if (e.Key == Key.Space)
                {
                    ResetGame();
                    return;

                }
            }
            else if (e.Key == Key.Left)
            {
                if (Ship.X - ShipVelocityX >= 0) Ship.X -= ShipVelocityX;
            }
            else if (e.Key == Key.Right)
            {
                if (Ship.X + Ship.Width + ShipVelocityX <= BoardWidth) Ship.X += ShipVelocityX;
            }
            else if (e.Key == Key.Space)
            {
                BulletArray.Add(new Block(Ship.X + Ship.Width / 2 - BulletWidth / 2, Ship.Y, BulletWidth, BulletHeight, null));
                ShootSound?.Play();
            }
        }

        private void ResetGame()
        {
            GameOver = false;
            GameOverText.Visibility = Visibility.Collapsed;
            Ship.X = ShipStartX; 
            Ship.Y = ShipStartY;
            Score = 0; 
            Lives = 3; 
            Rounds = 1; 
            NextLifeScore = 10000;
            FragshipArray.Clear(); 
            AlienRedArray.Clear(); 
            AlienPinkArray.Clear(); 
            AlienCyanArray.Clear();
            BulletArray.Clear(); 
            EnemyBulletArray.Clear();
            CreateFragship(); 
            CreateRedAliens(); 
            CreatePinkAliens(); 
            CreateCyanAliens();
            BackgroundSound?.Loop();
            GameLoop.Start();
        }
    }
}