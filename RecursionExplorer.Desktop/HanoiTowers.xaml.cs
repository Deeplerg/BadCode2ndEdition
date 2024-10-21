using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RecursionExplorer.Desktop
{
    public partial class HanoiTowersWindow : Window
    {
        private List<Tuple<int, int>> moves; // Список перемещений
        private int currentMoveIndex; // Индекс текущего перемещения
        private CancellationTokenSource cancellationTokenSource;
        private const int MaxDiscs = 30; // Максимальное количество колец
        private Stack<int>[] towers; // Массив башен
        private const int BaseTowerWidth = 10; // Базовая ширина башни
        private const int TowerHeight = 300; // Фиксированная высота башни
        private const int RingHeight = 20; // Высота кольца
        private double cmp;
        public HanoiTowersWindow()
        {
            InitializeComponent();
            moves = new List<Tuple<int, int>>();
            currentMoveIndex = 0;
            cancellationTokenSource = new CancellationTokenSource();
            towers = new Stack<int>[3]; // Инициализация массива башен
            for (int i = 0; i < towers.Length; i++)
            {
                towers[i] = new Stack<int>(); // Создаем стек для каждой башни
            }
            cmp = Width / 4;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            NextBut.IsEnabled = true;
            PrevBut.IsEnabled = true;
            int numberOfDiscs;
            if (int.TryParse(DiscCountTextBox.Text, out numberOfDiscs) && numberOfDiscs > 0 && numberOfDiscs <= MaxDiscs)
            {
                ResetGame();

                // Заполняем кольца на первой башне от большего к меньшему
                for (int i = numberOfDiscs; i >= 1; i--) // Изменено на от большего к меньшему
                {
                    towers[0].Push(i);
                }

                SolveHanoi(numberOfDiscs, 0, 2, 1); // Индексы башен начинаются с 0
                ExecutionTimeTextBlock.Text = $"{moves.Count}"; // Отображаем количество шагов
                DrawDiscs(); // Рисуем кольца
            }
            else
            {
                MessageBox.Show($"Введите корректное число колец (1 - {MaxDiscs}).");
            }
        }

        private void SolveHanoi(int n, int source, int target, int auxiliary)
        {
            if (n == 1)
            {
                moves.Add(new Tuple<int, int>(source, target));
            }
            else
            {
                SolveHanoi(n - 1, source, auxiliary, target);
                moves.Add(new Tuple<int, int>(source, target));
                SolveHanoi(n - 1, auxiliary, target, source);
            }
        }

        private async void SolveCompletelyButton_Click(object sender, RoutedEventArgs e)
        {
            if (moves.Count == 0)
            {
                MessageBox.Show("Сначала запустите задачу.");
                return;
            }
            else if (currentMoveIndex != 0)
            {
                MessageBox.Show("Сначала завершите предыдущую задачу.");
                return;
            }
            NextBut.IsEnabled = false;
            PrevBut.IsEnabled = false;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = moves.Count;

            for (int i = 0; i < moves.Count; i++)
            {
                currentMoveIndex = i; // Обновляем индекс текущего шага
                var move = moves[i];
                MoveDisc(move.Item1, move.Item2);

                ProgressBar.Value = i + 1;
                await Task.Delay(200); // Задержка для визуализации
            }

            stopwatch.Stop();
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void SolveQuicklyButton_Click(object sender, RoutedEventArgs e)
        {

            if (moves.Count == 0)
            {
                MessageBox.Show("Сначала запустите задачу.");
                return;
            }
            else if (currentMoveIndex != 0)
            {
                MessageBox.Show("Сначала завершите предыдущую задачу.");
                return;
            }
            NextBut.IsEnabled = false;
            PrevBut.IsEnabled = false;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = moves.Count;

            // Выполнение перемещения колец без задержки
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                MoveDisc(move.Item1, move.Item2);
                ProgressBar.Value = i + 1; // Обновляем значение прогресс бара
            }

            stopwatch.Stop();
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void DrawDiscs()
        {
            TowersCanvas.Children.Clear(); // Очищаем холст для новой отрисовки
            DrawTowers(); // Снова рисуем башни

            // Расчет расстояния между башнями
            double towerSpacing = (this.Width) / 3; // Распределение по ширине окна

            // Рисуем кольца на основе текущего состояния башен
            for (int i = 0; i < towers.Length; i++)
            {
                // Получаем кольца для текущей башни
                int offset = 0; // Смещение для расположения колец

                // Рисуем кольца на текущей башне
                foreach (var disc in towers[i].AsEnumerable().Reverse())
                {
                    Rectangle ring = new Rectangle
                    {
                        Width = disc * 20, // Ширина кольца (умножаем на 20 для увеличения размера)
                        Height = RingHeight,
                        Fill = Brushes.Blue,
                        StrokeThickness = 1,
                        Stroke = Brushes.Black,
                        RadiusX = 20,
                        RadiusY = 20
                    };

                    // Расположение кольца
                    Canvas.SetLeft(ring, cmp + i * towerSpacing - (ring.Width / 2) + 5); // Устанавливаем кольца на башню
                    Canvas.SetBottom(ring, RingHeight * offset); // Устанавливаем высоту для нового кольца

                    TowersCanvas.Children.Add(ring);
                    offset++; // Увеличиваем смещение для следующего кольца
                }
            }
        }

        private void DrawTowers()
        {
            // Очищаем холст
            TowersCanvas.Children.Clear();

            // Расчет расстояния между башнями

            double towerSpacing = Width / 3; // Распределение по ширине окна

            // Рисуем три башни с фиксированной высотой
            for (int i = 0; i < 3; i++)
            {
                Rectangle tower = new Rectangle
                {
                    Width = BaseTowerWidth,
                    Height = TowerHeight, // Фиксированная высота башни
                    Fill = Brushes.Gray
                };
                Canvas.SetLeft(tower, cmp + i * towerSpacing); // Расположение башен по горизонтали
                Canvas.SetBottom(tower, 0); // Устанавливаем снизу холста
                TowersCanvas.Children.Add(tower);
            }
        }

        private void ResetGame()
        {
            // Очистка состояния игры
            currentMoveIndex = 0;
            moves.Clear();
            ProgressBar.Value = 0;

            // Очищаем башни
            for (int i = 0; i < towers.Length; i++)
            {
                towers[i].Clear();
            }

            DrawTowers(); // Перерисовываем башни
            DrawDiscs(); // Обновляем кольца
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame(); // Очистка состояния
        }

        private void NextStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMoveIndex < moves.Count)
            {
                var move = moves[currentMoveIndex];
                MoveDisc(move.Item1, move.Item2);
                currentMoveIndex++;
                ProgressBar.Value = currentMoveIndex; // Обновляем прогресс бар
            }
            else
            {
                MessageBox.Show("Нет дополнительных шагов.");
            }
        }

        private void PreviousStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMoveIndex > 0)
            {
                currentMoveIndex--;
                var move = moves[currentMoveIndex];
                MoveDisc(move.Item2, move.Item1);
                ProgressBar.Value = currentMoveIndex; // Обновляем прогресс бар
            }
            else
            {
                MessageBox.Show("Нет предыдущих шагов.");
            }
        }

        private void MoveDisc(int from, int to)
        {
            if (towers[from].Count > 0) // Проверяем, есть ли кольца на башне
            {
                int disc = towers[from].Pop(); // Убираем верхнее кольцо с исходной башни
                towers[to].Push(disc); // Добавляем кольцо на целевую башню
                DrawDiscs(); // Обновляем отрисовку колец
            }
        }
    }
}
