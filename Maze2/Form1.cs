using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Maze2
{
    /// <summary>
    /// Nom: Paulemon
    /// Prenom: Christopher
    /// Class : Intelligence Artificielle
    /// Devoir : 4
    /// </summary>
    public partial class Form1 : Form
    {
        private PictureBox mazePictureBox;
        private Label numExploredLabel;
        private TextBox solutionTextBox;
        private Button loadMazeButton;

        private Maze maze;

        public Form1()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Créer le PictureBox pour afficher le labyrinthe
            mazePictureBox = new PictureBox();
            mazePictureBox.Location = new Point(10, 10);
            mazePictureBox.Size = new Size(800, 800);
            this.Controls.Add(mazePictureBox);

            // Créer le Label pour afficher le nombre d'états explorés
            numExploredLabel = new Label();
            numExploredLabel.Location = new Point(10, 520);
            numExploredLabel.AutoSize = true;
            this.Controls.Add(numExploredLabel);

            
            // Créer le bouton pour charger le labyrinthe
            loadMazeButton = new Button();
            loadMazeButton.Text = "Load Maze";
            loadMazeButton.Size = new Size(120, 40);
            loadMazeButton.Location = new Point(10, 860);
            loadMazeButton.Click += loadMazeButton_Click;
            this.Controls.Add(loadMazeButton);
        }

        private void LoadMaze(string filename)
        {
            try
            {
                // Libérer les ressources de l'image existante
                if (mazePictureBox.Image != null)
                {
                    mazePictureBox.Image.Dispose();
                    mazePictureBox.Image = null;
                }

                // Charger le nouveau labyrinthe
                maze = new Maze(filename);
                maze.Solve();

                // Spécifier le chemin complet au nouveau fichier image du labyrinthe
                string imagePath = Path.Combine(Application.StartupPath, "maze.png");

                // Générer et sauvegarder l'image du labyrinthe
                maze.OutputImage(imagePath, showExplored: true);

                // Charger l'image du labyrinthe dans le PictureBox
                using (Bitmap originalImage = new Bitmap(imagePath))
                {
                    // Définir la taille de l'image agrandie
                    int enlargedWidth = originalImage.Width * 2; // Ajustez la largeur comme vous le souhaitez
                    int enlargedHeight = originalImage.Height * 2; // Ajustez la hauteur comme vous le souhaitez

                    // Créer une nouvelle image agrandie
                    Bitmap enlargedImage = new Bitmap(enlargedWidth, enlargedHeight);

                    // Dessiner l'image originale agrandie dans l'image agrandie
                    using (Graphics g = Graphics.FromImage(enlargedImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(originalImage, new Rectangle(0, 0, enlargedWidth, enlargedHeight));
                    }

                    // Centrer l'image agrandie dans le PictureBox
                    int x = (mazePictureBox.Width - enlargedWidth) / 2;
                    int y = (mazePictureBox.Height - enlargedHeight) / 2;

                    // Afficher l'image agrandie dans le PictureBox
                    mazePictureBox.Image = enlargedImage;
                    mazePictureBox.Location = new Point(x, y);
                    mazePictureBox.SizeMode = PictureBoxSizeMode.AutoSize; // Redimensionner automatiquement le PictureBox en fonction de la taille de l'image
                }

                // Mettre à jour le texte affichant le nombre d'états explorés
                numExploredLabel.Text = $"States Explored: {maze.NumExplored}";

                // Afficher la solution dans le TextBox
                solutionTextBox.Text = maze.GetSolutionString();
            }
            catch (Exception ex)
            {
              //  MessageBox.Show($"An error occurred while loading the maze: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void loadMazeButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt";
            openFileDialog.Title = "Select Maze File";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadMaze(openFileDialog.FileName);
            }
        }
    }
}


public class Node
    {
        public Point State { get; set; }
        public Node Parent { get; set; }
        public string Action { get; set; }
    }

    public class StackFrontier
    {
        private List<Node> frontier;

        public StackFrontier()
        {
            frontier = new List<Node>();
        }

        public void Add(Node node)
        {
            frontier.Add(node);
        }

        public bool ContainsState(Point state)
        {
            foreach (var node in frontier)
            {
                if (node.State == state)
                    return true;
            }
            return false;
        }

        public bool Empty()
        {
            return frontier.Count == 0;
        }

        public Node Remove()
        {
            if (Empty())
                throw new Exception("empty frontier");

            Node node = frontier[frontier.Count - 1];
            frontier.RemoveAt(frontier.Count - 1);
            return node;
        }
    }

public class Maze
{
    private Point start;
    private Point goal;
    private int height;
    private int width;
    private bool[][] walls;
    private (List<string>, List<Point>) solution;
    private HashSet<Point> explored;
    private StackFrontier frontier;
    private int num_explored;


    public int NumExplored { get { return num_explored; } }

    public Maze(string filename)
    {
        // Read file and set height and width of maze
        string[] lines = File.ReadAllLines(filename);

        // Validate start and goal
        int startCount = 0, goalCount = 0;
        foreach (string line in lines)
        {
            foreach (char c in line)
            {
                if (c == 'A') startCount++;
                else if (c == 'B') goalCount++;
            }
        }
        if (startCount != 1)
            throw new Exception("maze must have exactly one start point");
        if (goalCount != 1)
            throw new Exception("maze must have exactly one goal");

        // Determine height and width of maze
        height = lines.Length;
        width = 0;
        foreach (string line in lines)
        {
            width = Math.Max(width, line.Length);
        }

        // Keep track of walls
        walls = new bool[height][];
        for (int i = 0; i < height; i++)
        {
            walls[i] = new bool[width];
            for (int j = 0; j < width; j++)
            {
                if (j < lines[i].Length)
                {
                    if (lines[i][j] == 'A')
                    {
                        start = new Point(i, j);
                        walls[i][j] = false;
                    }
                    else if (lines[i][j] == 'B')
                    {
                        goal = new Point(i, j);
                        walls[i][j] = false;
                    }
                    else if (lines[i][j] == ' ')
                    {
                        walls[i][j] = false;
                    }
                    else
                    {
                        walls[i][j] = true;
                    }
                }
                else
                {
                    walls[i][j] = false;
                }
            }
        }
    }

    public List<(string, Point)> Neighbors(Point state)
    {
        int row = state.X;
        int col = state.Y;
        var candidates = new List<(string, Point)>()
    {
        ("up", new Point(row - 1, col)),
        ("down", new Point(row + 1, col)),
        ("left", new Point(row, col - 1)),
        ("right", new Point(row, col + 1))
    };

        var result = new List<(string, Point)>();
        foreach (var candidate in candidates)
        {
            string action = candidate.Item1;
            Point neighbor = candidate.Item2;

            int r = neighbor.X;
            int c = neighbor.Y;

            if (r >= 0 && r < height && c >= 0 && c < width && !walls[r][c])
            {
                result.Add((action, neighbor));
            }
        }
        return result;
    }


    public void Solve()
    {
        // Keep track of number of states explored
        num_explored = 0;

        // Initialize frontier to just the starting position
        Node startNode = new Node() { State = start, Parent = null, Action = null };
        frontier = new StackFrontier();
        frontier.Add(startNode);

        // Initialize an empty explored set
        explored = new HashSet<Point>();

        // Keep looping until solution found
        while (true)
        {
            // If nothing left in frontier, then no path
            if (frontier.Empty())
                throw new Exception("no solution");

            // Choose a node from the frontier
            Node node = frontier.Remove();
            num_explored++;

            // If node is the goal, then we have a solution
            if (node.State == goal)
            {
                var actions = new List<string>();
                var cells = new List<Point>();
                while (node.Parent != null)
                {
                    actions.Add(node.Action);
                    cells.Add(node.State);
                    node = node.Parent;
                }
                actions.Reverse();
                cells.Reverse();
                solution = (actions, cells);
                return;
            }

            // Mark node as explored
            explored.Add(node.State);

            // Add neighbors to frontier
            foreach (var (action, state) in Neighbors(node.State))
            {
                if (!frontier.ContainsState(state) && !explored.Contains(state))
                {
                    Node child = new Node() { State = state, Parent = node, Action = action };
                    frontier.Add(child);
                }
            }
        }
    }

    public string GetSolutionString()
    {
        if (ReferenceEquals(solution, null))
            return "No solution found.";

        string solutionString = "";
        for (int i = 0; i < solution.Item1.Count; i++)
        {
            solutionString += $"{solution.Item1[i]} -> {solution.Item2[i]}\n";
        }
        return solutionString;
    }


    public void OutputImage(string filename, bool showSolution = true, bool showExplored = false)
    {
        int cellSize = 20;
        int cellBorder = 2;

        using (Bitmap bmp = new Bitmap(width * cellSize, height * cellSize))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                var solutionPoints = !ReferenceEquals(solution, null) ? new HashSet<Point>(solution.Item2) : null;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        Brush fill;
                        if (walls[i][j])
                        {
                            fill = Brushes.Gray;
                        }
                        else if (start.X == i && start.Y == j)
                        {
                            fill = Brushes.Red;
                        }
                        else if (goal.X == i && goal.Y == j)
                        {
                            fill = Brushes.Green;
                        }
                        else if (showSolution && solutionPoints != null && solutionPoints.Contains(new Point(i, j)))
                        {
                            fill = Brushes.Yellow;
                        }
                        else if (showExplored && explored.Contains(new Point(i, j)))
                        {
                            fill = Brushes.Orange;
                        }
                        else
                        {
                            fill = Brushes.White;
                        }

                        g.FillRectangle(fill, j * cellSize + cellBorder, i * cellSize + cellBorder,
                                        cellSize - 2 * cellBorder, cellSize - 2 * cellBorder);
                    }
                }
            }

            bmp.Save(filename);
        }
    }
}