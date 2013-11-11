using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genetic_Algorithm
{
    class Program
    {
        static void Main(string[] args)
        {
            using (System.IO.StreamWriter output = new System.IO.StreamWriter("ExperimentResults.txt"))
            {
                for (var i = 0; i < 50; ++i)
                {
                    var guesses = 0;
                    MastermindGame puzzle = new MastermindGame(4, 9);
                    MastermindAI player = new MastermindAI(puzzle);
                    var result = player.Solve(ref guesses);
                    output.WriteLine("Solution {0}found{1}", result ? "" : "not ",
                        result ? string.Format(" in {0} guesses", guesses) : "");
                }
            }
        }
    }

    class MastermindGame
    {
        private Random rand = new Random();
        private string code { get; set; }
        public string[] guesses = new string[10];
        private Dictionary<int, int[]> pegScores = new Dictionary<int, int[]>();
        public int nGuesses = 0;
        public int colors { get; set; }
        public int pegs { get; set; }

        public MastermindGame(int nPegs, int nColors)
        {
            colors = nColors;
            pegs = nPegs;
            code = generateCodes();
            //Console.WriteLine("Puzzle to solve: {0}", code);
        }

        public string generateCodes()
        {
            string l_code = "";
            for (var i = 0; i < pegs; ++i)
                l_code += rand.Next(1, colors);

            return l_code;
        }

        public int calcScore(string puzzle)
        {
            int score = 0;
            int count = 0;
            /// Use these to track which positions trigger points so they aren't counted twice
            var flags1 = new int[4] { -1, -1, -1, -1 };
            var flags2 = new int[4] { -1, -1, -1, -1 };
            int[] result = new int[] { 0, 0, 0, 0 };
            /// Add 10 points for each right color/right position
            for (var i = 0; i < puzzle.Length; ++i)
            {
                if (puzzle[i] == code[i])
                {
                    score += 10;
                    result[count++] = 1;
                    flags1[i] = 1;
                    flags2[i] = 1;
                }
            }

            if (score == 40)
            {
                return score;
            }

            /// Add 1 point for each correct color/wrong position
            for (var i = 0; i < puzzle.Length; ++i)
            {
                for (var j = 0; j < puzzle.Length; ++j)
                {
                    /// Don't count right color/position twice
                    if (i != j)
                    {
                        if (flags1[i] != 1 && flags2[j] != 1 && puzzle[i] == code[j])
                        {
                            score += 1;
                            result[count++] = 2;
                            flags1[i] = 1;
                            flags2[j] = 1;
                            break;
                        }
                    }
                }
            }

            guesses[nGuesses] = puzzle;
            pegScores.Add(nGuesses++, result);

            return score;
        }

        public int[] calcFitness(string puzzle, int row)
        {
            var score = 0;
            var count = 0;
            var result = new int[4] { 0, 0, 0, 0 };
            var flags1 = new int[4] { -1, -1, -1, -1 };
            var flags2 = new int[4] { -1, -1, -1, -1 };

            for (var i = 0; i < puzzle.Length; ++i)
            {
                if (puzzle[i] == guesses[row][i])
                {
                    score += 10;
                    result[count++] = 1;
                    flags1[i] = 1;
                    flags2[i] = 1;
                }
            }

            if (score == 40)
            {
                return result;
            }

            /// Add 1 points for each correct color/wrong position
            for (var i = 0; i < puzzle.Length; ++i)
            {
                for (var j = 0; j < puzzle.Length; ++j)
                {
                    /// Don't count right color/position twice
                    if (i != j)
                    {
                        if (flags1[i] != 1 && flags2[j] != 1 && puzzle[i] == guesses[row][j])
                        {
                            result[count++] = 2;
                            flags1[i] = 1;
                            flags2[j] = 1;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private int compareToPredecessor(int[] first, int second)
        {
            var matches = 0;

            for (var i = 0; i < first.Length; ++i)
            {
                if (first[i] == pegScores[second][i])
                {
                    matches++;
                }
            }

            return matches;
        }

        public double fitnessFromPrior(Gene guess)
        {
            var fitness = 0.0D;

            for (var i = 0; i < nGuesses; ++i)
            {
                var result = calcFitness(guess.value, i);
                var comparisonScore = compareToPredecessor(result, i);
                fitness += comparisonScore / 4.0D;
            }

            fitness += 0.1D;
            
            return fitness;
        }
    }

    class MastermindAI
    {
        List<Gene> population = new List<Gene>();
        MastermindGame puzzleBoard;
        double Temperature = 0.2;
        static int popSize = 1000;
        static int toSelect = (int)(popSize * 0.65);
        static int toMutate = toMutate = (int)(toSelect * 0.10);
        static int toCross = (int)(0.35 * popSize);

        public MastermindAI(MastermindGame puzzle)
        {
            puzzleBoard = puzzle;
        }

        public bool Solve(ref int nGuesses)
        {
            var success = false;
            for (var i = 0; i < 10; ++i)
            {
                population = generateTestPopulation();
                var guess = GenerateGuess();
                var result = puzzleBoard.calcScore(guess);
                var blackPegs = result/10;
                var whitePegs = result % 10;
                //Console.WriteLine("Guess: {0} - Value:{1}{2}", guess, new string('b', blackPegs), new string('w', whitePegs));
                if (result == 40)
                {
                    nGuesses = puzzleBoard.nGuesses;
                    success = true;
                    break;
                }
            }
            return success;
        }

        private string GenerateGuess()
        {
            var code = "";

            population.Sort((x, y) => y.fitness.CompareTo(x.fitness));
            if (population.Count > popSize)
                population.RemoveRange(popSize - 1, population.Count - popSize);
            Gene test = population.Find(x => x.value.Equals("7482"));
            code = population[0].value;

            return code;
        }

        private List<Gene> generateTestPopulation()
        {
            List<Gene> pop = new List<Gene>();
            List<string> codes = new List<string>();
            for (var i = 0; i < popSize; ++i)
            {
                Gene code = null;
                while (code == null || codes.Contains(code.value))
                {
                    code = new Gene(puzzleBoard.generateCodes());
                }

                codes.Add(code.value);
                code.crossover = 2;
                code.fitness = puzzleBoard.fitnessFromPrior(code);
                pop.Add(code); 
            }

            pop.Sort((x, y) => y.fitness.CompareTo(x.fitness));
            double test = puzzleBoard.fitnessFromPrior(new Gene("6665"));
            pop = generation(pop);

            return pop;
        }

        private List<Gene> generation(List<Gene> pop)
        {
            List<Gene> newPop = new List<Gene>();
            List<int> mutated = new List<int>();
            var codes = new List<string>();
            var rand = new Random();

            newPop = select(pop, ref codes);
            newPop.AddRange(cross(pop, ref codes));
            while (mutated.Count < toMutate)
            {
                var gene = rand.Next(newPop.Count);
                if (!mutated.Contains(gene))
                {
                    newPop[gene].Mutate(puzzleBoard.colors, puzzleBoard.pegs);
                    newPop[gene].fitness = puzzleBoard.fitnessFromPrior(newPop[gene]);
                    mutated.Add(gene);
                }
            }

            return newPop;
        }

        private List<Gene> select(List<Gene> pop, ref List<string> codes)
        {
            var selected = new List<Gene>();
            var chance = new List<List<object>>();
            var rand = new Random();
            var totalFitness = 0.0;

            foreach (var g in pop)
            {
                totalFitness += g.fitness;
            }

            foreach (var g in pop)
            {
                var prob = g.fitness / totalFitness;
                chance.Add(new List<object>() {prob, g});
            }

            var nSelected = 0;
            while (nSelected < toSelect)
            {
                foreach (var g in chance)
                {
                    var nextRand = rand.NextDouble();
                    if (nextRand <= (double)g[0])
                    {
                        selected.Add((Gene)g[1]);
                        codes.Add(selected[selected.Count - 1].value);
                        nSelected++;
                    }
                }
            }

            return selected;
        }

        private List<Gene> cross(List<Gene> pop, ref List<string> codes)
        {
            var parent1 = new List<Gene>();
            var parent2 = new List<Gene>();
            var result = new List<Gene>();
            var chance = new List<List<object>>();
            var rand = new Random();
            var totalFitness = 0.0;

            foreach (var g in pop)
            {
                totalFitness += g.fitness;
            }

            foreach (var g in pop)
            {
                var prob = g.fitness / totalFitness;
                chance.Add(new List<object>() { prob, g });
            }

            var nSelected = 0;
            while (nSelected < toCross)
            {
                foreach (var g in chance)
                {
                    var nextRand = rand.NextDouble();
                    if (nextRand <= (double)g[0])
                    {
                        if (rand.Next(100) % 2 > 0)
                        {
                            parent1.Add((Gene)g[1]);
                        }
                        else
                        {
                            parent2.Add((Gene)g[1]);
                        }
                        nSelected++;
                    }
                }
            }

            if (parent1.Count > parent2.Count)
            {
                while (parent1.Count > parent2.Count)
                {
                    parent2.Add(parent1[parent1.Count - 1]);
                    parent1.RemoveAt(parent1.Count - 1);
                }

                if (parent2.Count > parent1.Count)
                    parent2.RemoveAt(parent2.Count - 1);
            }
            else
            {
                while (parent2.Count > parent1.Count)
                {
                    parent1.Add(parent2[parent2.Count - 1]);
                    parent2.RemoveAt(parent2.Count - 1);
                }
                if (parent1.Count > parent2.Count)
                    parent1.RemoveAt(parent1.Count - 1);
                
            }

            for (var i = 0; i < parent1.Count; ++i)
            {
                var offspring = parent1[i].Crossover(parent2[i]);
                foreach (var c in offspring)
                {
                    while (codes.Contains(c.value))
                    {
                        c.value = puzzleBoard.generateCodes();
                    }
                    c.fitness = puzzleBoard.fitnessFromPrior(c);
                    codes.Add(c.value);
                }
                result.AddRange(offspring);
            }

            return result;
        }
    }

    class Gene
    {
        public string value { get; set; }
        public int crossover { get; set; }
        public double fitness { get; set; }

        public Gene(string code)
        {
            value = code;
        }

        public void Mutate(int nColors, int nPegs)
        {
            var point = new Random();
            var color = new Random();
            var mutation = value.ToCharArray();
            var newPoint = point.Next(nPegs);
            var newColor = 0;
            while (newColor == 0 || newColor == value[newPoint])
            {
                newColor = color.Next(1, nColors);
            }
            mutation[newPoint] = newColor.ToString().ToCharArray()[0];

            this.value = new string(mutation);
        }

        public List<Gene> Crossover(Gene parent2)
        {
            var p1a = this.value.Substring(0, value.Length - crossover);
            var p1b = this.value.Substring(value.Length - crossover, value.Length - crossover);
            var p2a = parent2.value.Substring(0, value.Length - crossover);
            var p2b = parent2.value.Substring(value.Length - crossover, value.Length - crossover);
            var child1 = new Gene(p1a + p2b);
            var child2 = new Gene(p2a + p1b);

            child1.crossover = this.crossover;
            child2.crossover = this.crossover;

            return new List<Gene>() { child1, child2 };
        }
    }
}
