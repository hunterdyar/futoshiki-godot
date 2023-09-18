using System;
using System.Collections;
using System.Collections.Generic;
using Godot;


public class Board
{
    public enum GameType
    {
        Futoshiki,
        Renzoku
    }
    public struct Comparison
    {
        public (int, int) pos1;
        public (int, int) pos2;
        public Comparator comparator;

        public Comparison((int, int) a, (int, int) b, Comparator c)
        {
            pos1 = a;
            pos2 = b;
            comparator = c;
        }
    }

    public struct ValRef
    {
        public int x;
        public int y;
        public int val;

        public ValRef(int x, int y, int v)
        {
            this.x = x;
            this.y = y;
            val = v;
        }
    }

    public enum Comparator
    {
        GreaterThan,
        LessThan,
        Adjacent,
        NonAdjacent,
        Equal,//this one means ERROR
    }

    public GameType gameType;
    public int size;
    public int[,] grid;
    public bool[,] givenValues;
    public bool[,] validSpaceCheck;
    public BitArray[,] notes;//bitmask!
    public List<Comparison> comparisons;
    public string original;
    public bool isSolved;//only set on initiation and manual checking, not automatically!
    public Dictionary<((int, int), (int, int)), Comparison> compLookupTable;//Tuple of Tuples.
    
    
    public static System.Random mran = new System.Random();

    //Generator things
    private List<Comparison> eligibleComparisons;
    private List<ValRef> eligibleValues;
    public Board(int size = 0)
    {
        if (size != 0)
        {
            Init(size);
        }
    }

    
    
    public void Init(int size, GameType type = GameType.Futoshiki)
    {
        System.Random mran = new System.Random();
        gameType = type;
        comparisons = new List<Comparison>();
        grid = new int[size,size];
        givenValues = new bool[size,size];
        notes = new BitArray[size, size];
        validSpaceCheck = new bool[size,size];
        isSolved = false;
        this.size = size;
        //Initiate an empty grid.
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                grid[i, j] = 0;
                givenValues[i, j] = false;
                notes[i, j] = new BitArray(size);
                validSpaceCheck[i, j] = true;
            }
        }
    }

    
    
    public string LevelToString(bool serializeNotes = false)
    {

        string level = size + ":";

        int count = 0;
        for (int i = size - 1; i >= 0; i--)
        {
            for (int j = 0; j < size; j++) //we read levels like a book.
            {
                int val = grid[j, i];
                Comparator cr = FindComparator(j, i, j + 1, i);
                Comparator cd = FindComparator(j, i, j, i - 1);
                int note = BitArrayToInt(notes[j, i]);
                if (val == 0 && cr == Comparator.Equal && cd == Comparator.Equal && (!serializeNotes || note == 0))
                {
                    count++;
                }
                else
                {
                    //COmparator will return "" if invalid, so we dont need safety check.
                    level = level + IntToString(count);//how many spaces we have moved.
                    level = level + ComparatorToString(cr, true);
                    level = level + ComparatorToString(cd, false);
                    if (note != 0)
                    {
                        level = level + "[" + note + "]";
                    }
                    level = level + val.ToString();
                    count = 0;
                }
            }
        }
        return level;
    }

   

    public void LoadFromString(string puzzleCode)
    {
        string[] puzz = puzzleCode.Split(':');
        if (puzz.Length > 2 || puzz.Length == 0)
        {
            GD.PrintErr("Invalid puzzleCode!");
            return;
        }

        bool noteMode = false;
        size = int.Parse(puzz[0]);
        Init(size);

        String level = puzz[1];
        string noteString = "";
        int count = 0;
        for (int i = 0; i < level.Length; i++)
        {
            char c = level[i];
            (int, int) pos = GetPosFromCount(count, size);

            if (c == '[')
            {
                noteMode = true;
                continue;//goNext
            }

            if (noteMode)
            {
                if (c == ']')
                {
                    noteMode = false;
                    int noteI;
                    if (int.TryParse(noteString, out noteI))
                    {
                        notes[pos.Item1,pos.Item2] = IntToBitArray(noteI,size);//works for 1.
                    }
                    noteString = "";
                    continue;
                }
                else
                {
                    //note numbers may be > 9
                    noteString = noteString + c.ToString();
                }
            }

            if (!noteMode)
            {
                int val;
                if (int.TryParse(c.ToString(), out val))
                {
                    grid[pos.Item1, pos.Item2] = val;
                    count++;
                }
                else
                {
                    (int, int) posR = (pos.Item1 + 1, pos.Item2);
                    (int, int) posD = (pos.Item1, pos.Item2 - 1);
                    switch (c)
                    {
                        case 'v':
                            AddComparator(pos, posD, Comparator.GreaterThan);
                            break;
                        case '^':
                            AddComparator(pos, posD, Comparator.LessThan);
                            break;
                        case '<':
                            AddComparator(pos, posR, Comparator.LessThan);
                            break;
                        case '>':
                            AddComparator(pos, posR, Comparator.GreaterThan);
                            break;
                        case '.':
                            AddComparator(pos, posR, Comparator.Adjacent);
                            break;
                        case '*':
                            AddComparator(pos, posD, Comparator.Adjacent);
                            break;
                        default: //We dont have an int, and we dont have a comparison, we must have a counter character.
                            int countUp = CharToInt(c);
                            count = count + countUp;
                            break;
                    }
                }
            }
        }

        if (CountFutoshikiComparators() == 0 && comparisons.Count > 0)
        {
            //we can infer that if we have any > or < rules, then this isnt a pure renzoku puzzle.
            gameType = GameType.Renzoku;
            FillRenzokuComparisons();//fill any spots that arent defined as adjacent with "nonadjacent".
        }
        else
        {
            gameType = GameType.Futoshiki;
        }
        SetGivenFromCurrentGrid();
        original = puzzleCode;
    }

    int CountFutoshikiComparators()
    {
        int count = 0;
        foreach (Comparison c in comparisons)
        {
            if (c.comparator == Comparator.GreaterThan || c.comparator == Comparator.LessThan)
            {
                count++;
            }
        }

        return count;
    }
    public void ToggleNote(int x, int y, int val,bool unsetValue = true)
    {
        //cancel out of input after game is won. Input shouldnt call this but stopping some odd edge case.
        if (isSolved)
        {
            return;
        }
        
        if (x < 0 || y < 0 || x >= size || y >= size || val<=0)
        {
            GD.PrintErr("Invalid Location");
        }

        notes[x, y][val-1] = !notes[x, y][val-1];
        
        if (unsetValue)//if Operating on this space should override our value, hiding it.
        {
            grid[x, y] = 0;
        }
    }

    public BitArray GetNoteBitArray(int x, int y)
    {
        return notes[x, y];
    }

    public bool GetNote(int x, int y, int note)
    {
        return notes[x, y][note - 1];
    }

    public void SetValue(int x, int y, int val, bool given = false)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            GD.PrintErr("Invalid Location");
        }

        grid[x,y] = val;
        givenValues[x, y] = given;
    }

    public int GetValue(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            GD.PrintErr("Invalid Location");
        }

        if (grid == null)
        {
            return 0;
        }
        return grid[x,y];
    }
    public bool GetGiven(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            GD.PrintErr("Invalid Location");
        }

        if (grid == null)
        {
            return false;
        }
        
        return givenValues[x,y];
    }

    public Comparator FindComparator(int x1, int y1, int x2, int y2)
    {
        if (x1 < 0 || y1 < 0 || x2 < 0 || y2 < 0)
        {
            return Comparator.Equal;
        }

        if (x1 >= size || y1 >= size || y2 >= size || x2 >= size)
        {
            return Comparator.Equal;
        }
        //
        
        
        (int, int) p1 = (x1, y1);
        (int, int) p2 = (x2, y2);
        foreach (Comparison com in comparisons)
        {
            if (com.pos1 == p1 && com.pos2 == p2)
            {
                return com.comparator;
            }else if (com.pos1 == p2 && com.pos2 == p1)
            {
                if (com.comparator == Comparator.GreaterThan)
                {
                    return Comparator.LessThan;
                }
                else if (com.comparator == Comparator.LessThan)
                {
                    return Comparator.GreaterThan;
                }
                return com.comparator;
            }
        }
        return Comparator.Equal;//Equal is basically Null, since things should never be set to this.
    }

    public Comparison AddComparator((int, int) pos1, (int, int) pos2, Comparator comp)
    {
        Comparison c = new Comparison(pos1,pos2,comp);
        comparisons.Add(c);
        return c;
    }
    public Comparison AddComparator(int pos1x, int pos1y, int pos2x,int pos2y, Comparator comp)
    {
        Comparison c = new Comparison((pos1x,pos1y), (pos2x,pos2y),comp);
        comparisons.Add(c);
        return c;
    }

    public void UpdateValidSpaces()
    {
        //TODO refactor this. We only need to loop through the comparisons once. 

        int invalidRow = -1;
        int invalidCol = -1;
        //Check Rows
        for (int i = 0; i < size; i++)
        {
            int[] counts = new int[size + 1]; //defaults to 0
            for (int j = 0; j < size; j++)
            {
                int val = grid[i, j];
                if (val == 0)
                {
                    continue;
                }
                counts[val] += 1;
                if (counts[val] >= 2)
                {
                    invalidRow = i;
                    // break;
                }
            }
        }

        //Check Cols
        //Check Cols
        for (int j = 0; j < size; j++)
        {
            int[] counts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                int val = grid[i, j];
                if (val == 0)
                {
                    continue;
                }
                counts[val] += 1;
                
                if (counts[val] >= 2)
                {
                    invalidCol = j;
                    break;
                }
            }
        }

        List<(int, int)> invalidComparisons = new List<(int, int)>();
        foreach (Comparison c in comparisons)
        {
            if(!IsComparisonValid(c))
            {
                if (!invalidComparisons.Contains(c.pos1))
                {
                    invalidComparisons.Add(c.pos1);
                }
                if (!invalidComparisons.Contains(c.pos2))
                {
                    invalidComparisons.Add(c.pos2);
                }
                // break;
            }
        }
        //Loop through and update the rows and columns.   
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i == invalidRow || j == invalidCol)
                {
                    validSpaceCheck[i, j] = false;
                }
                else
                {
                    validSpaceCheck[i, j] = true;
                }
            }
        }
        //Update invalid comparisons (assuming they are incorrectly set to valid)
        //We do this here because we only need to loop through this list once, instead of checking every space against it.
        foreach((int,int) ic in invalidComparisons)
        {
            validSpaceCheck[ic.Item1, ic.Item2] = false;
        }

        }

    public bool isValid()
    {
        //Check Latin squares.
        
        //Check Rows
        for (int i = 0; i < size; i++)
        {
            int[] counts = new int[size+1];//defaults to 0 right?
            for (int j = 0; j < size; j++)
            {
                int val = grid[i, j];
                counts[val] += 1;
                if (val == 0)
                {
                    isSolved = false;
                    return false;
                }
                if (counts[val] >= 2)
                {
                    isSolved = false;
                    return false;
                }
            }
        }
        //Check Cols
        for (int j = 0; j < size; j++)
        {
            int[] counts = new int[size+1];
            for (int i = 0; i < size; i++)
            {
                int val = grid[i, j];
                if (val == 0)
                {
                    isSolved = false;
                    return false;
                }
                counts[val] += 1;
                if (counts[val] >= 2)
                {
                    isSolved = false;
                    return false;
                }
            }
        }
        
        //Check Conditions
        //TODO are we not checking for ryz bugs
        foreach (Comparison c in comparisons)
        {
            //If the values are 0, then the grid is not invalid.
            if (grid[c.pos1.Item1,c.pos1.Item2] != 0 && grid[c.pos2.Item1,c.pos2.Item2] != 0)
            {
                if (c.comparator == Comparator.GreaterThan)
                {
                    if (grid[c.pos1.Item1,c.pos1.Item2]<= grid[c.pos2.Item1,c.pos2.Item2])
                    {
                        isSolved = false;
                        return false;
                    }
                }
                else if (c.comparator == Comparator.LessThan)
                {
                    if (grid[c.pos1.Item1,c.pos1.Item2] >= grid[c.pos2.Item1,c.pos2.Item2])
                    {
                        isSolved = false;
                        return false;
                    }
                }else if (c.comparator == Comparator.Adjacent)
                {
                    if (!Adjacent(grid[c.pos1.Item1,c.pos1.Item2], grid[c.pos2.Item1,c.pos2.Item2]))
                    {
                        isSolved = false;
                        return false;
                    }
                }else if (c.comparator == Comparator.NonAdjacent)
                {
                    if (Adjacent(grid[c.pos1.Item1,c.pos1.Item2], grid[c.pos2.Item1,c.pos2.Item2]))
                    {
                        isSolved = false;
                        return false;
                    }
                }
            }
        }

        isSolved = true;//apparentl this code was refactored to return false if incomplete. 
        return true;
    }

    private bool IsComparisonValid(Comparison c)
    {
        //If the values are 0, then the grid is not invalid.
        if (grid[c.pos1.Item1, c.pos1.Item2] == 0 || grid[c.pos2.Item1, c.pos2.Item2] == 0){
            return true;
        }
        
        
        if (c.comparator == Comparator.GreaterThan)
        {
            if (grid[c.pos1.Item1,c.pos1.Item2]>grid[c.pos2.Item1,c.pos2.Item2])
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (c.comparator == Comparator.LessThan)
        {
            if (grid[c.pos1.Item1,c.pos1.Item2] < grid[c.pos2.Item1,c.pos2.Item2])
            {
                return true;
            }
            else
            {
                return false;
            }
        }else if (c.comparator == Comparator.Adjacent)
        {
            return Adjacent(grid[c.pos1.Item1, c.pos1.Item2], grid[c.pos2.Item1, c.pos2.Item2]);
        }else if (c.comparator == Comparator.NonAdjacent)
        {
            return !Adjacent(grid[c.pos1.Item1, c.pos1.Item2], grid[c.pos2.Item1, c.pos2.Item2]);
        }

        //else!?!?
        return false;
    }
    public bool TrySolve(bool stopAtOne = false)
    {
        //Clone the grid.
        int[,] ngrid = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                ngrid[i, j] = grid[i,j];
            }
        }
        //Solve it.
        int solutions = 0;
        int s = Solve(ngrid, ref solutions,stopAtOne,true);
        return s == 1;
    }
    public int Solve(int[, ] sgrid, ref int solutions, bool stopAtOne = false,bool returnAfterOne=true)
    {
        //
        //
        int n = size;
        int row = -1; 
        int col = -1;
        
        bool isEmpty = true; 
        for (int i = 0; i < n; i++) { 
            for (int j = 0; j < n; j++) { 
                if (sgrid[i, j] == 0) { 
                    row = i; 
                    col = j; 
  
                    // we still have some remaining 
                    // missing values in Sudoku 
                    isEmpty = false; 
                    break; 
                } 
            } 
            if (!isEmpty) { 
                break; 
            } 
        } 
  
        //
        
        // no empty space left 
        if (isEmpty) {
            //return true, solved!
            solutions++;
            return solutions; 
        } 
        
        //This slows the generator down. It's just more checks, more loops - even if it can find the right number, its doing more work than just guessing them all.
        //Which fucking sucks lol.
        
        
        // int v;
        // if (HasNeccesary(sgrid, row, col, out v))
        // {
        //     sgrid[row, col] = v;
        //     if (Solve(sgrid, ref solutions) > 0)
        //     {
        //         if (stopAtOne) return solutions;
        //         if (returnAfterOne)
        //         {
        //             if (solutions > 1)
        //             {
        //                 return solutions;
        //             }
        //         }
        //     }
        //
        //     //we continue the search...
        //     sgrid[row, col] = 0;
        // }//
        
        
        
        // else for each-row backtrack 
        for (int num = 1; num <= n; num++)
        {
            if (IsSafe(sgrid, row, col, num)) { 
                sgrid[row, col] = num; 
                if (Solve(sgrid, ref solutions) > 0)
                {
                    //solutions++;
                    // print(board, n);
                    //return solutions
                    // return solutions;
                    if (stopAtOne)
                    {
                        return solutions;//we did it, we found what we wanted?
                    }

                    if (returnAfterOne)
                    {
                        if (solutions > 1)
                        {
                            return solutions;//we can end our search early if we are only looking for one unique solution. The instant we find a second solution: game over.
                        }
                    }
                    //keep going? I kind of hate this!
                    sgrid[row, col] = 0;
                } 
                else
                {
                    // replace it 
                    sgrid[row, col] = 0; 
                } 
            } 
        } 
        //return false
        return solutions; 
    }
    public bool IsSafe(int[, ] board,int row, int col, int num) 
    { 
        // row has the unique (row-clash) 
        for (int d = 0; d < board.GetLength(0); d++) { 
            // if the number we are trying to 
            // place is already present in 
            // that row, return false; 
            if (board[row, d] == num) { 
                return false; 
            } 
        } 
  
        // column has the unique numbers (column-clash) 
        for (int r = 0; r < board.GetLength(0); r++) { 
            // if the number we are trying to 
            // place is already present in 
            // that column, return false; 
            if (board[r, col] == num) { 
                return false; 
            } 
        } 
  
        foreach (Comparison c in comparisons)
        {
            //If the values are 0, then the grid is not invalid.
            if (board[c.pos1.Item1,c.pos1.Item2] != 0 && board[c.pos2.Item1,c.pos2.Item2] != 0)
            {
                if (c.comparator == Comparator.GreaterThan)
                {
                    if (board[c.pos1.Item1,c.pos1.Item2] <= board[c.pos2.Item1,c.pos2.Item2])
                    {
                        return false;
                    }
                }else if (c.comparator == Comparator.LessThan)
                {
                    if (board[c.pos1.Item1,c.pos1.Item2] >= board[c.pos2.Item1,c.pos2.Item2])
                    {
                        return false;
                    }
                }
                else if (c.comparator == Comparator.Adjacent)
                {
                    if (!Adjacent(board[c.pos1.Item1,c.pos1.Item2],board[c.pos2.Item1,c.pos2.Item2]))
                    {
                        return false;
                    }
                }else if (c.comparator == Comparator.NonAdjacent)
                {
                    if (Adjacent(board[c.pos1.Item1,c.pos1.Item2],board[c.pos2.Item1,c.pos2.Item2]))
                    {
                        return false;
                    }
                }
            }
        }
        
        // if there is no clash, it's safe 
        return true; 
    }
    
    public bool HasNeccesary(int[, ] grid, int row, int col, out int val)//Tries on specific number, but if we are trying to autocomplete a bunch of spaces, then a more efficient method is possibke.
    {
        val = -1;
        
        //
        int valsSet = 0;
        bool[] counts = new bool[size];//by default, all false.
        for(int c = 0;c<size;c++)
        {
            if(grid[row, c] != 0)
            {
                counts[grid[row, c]-1] = true;
                valsSet++;
            }
        }
        
        for(int r = 0;r<size;r++)
        {
            if (grid[r, col] !=0)
            {
                counts[grid[r, col]-1] = true;//true, or its already true, which is also fine.
                valsSet++;
            }
        }

        if (valsSet < 3)//easy early exit to avoid an extra loop, should speed up generation during the early stages.
        {
            return false;
        }
        //if row is 1,[],3,4 then counts should be tftt. If there is only 1 f, then we can logically assert the value from the array.

        int numFalse = 0;
        for(int i = 0;i<size;i++)
        {
            if (counts[i] == true)
            {
                numFalse++;
                if (numFalse > 1)
                {
                    return false;
                }
            }
            else
            {
                val = i+1;//the index of the un-set value is (1minus) the available value.
            }
        }

        if (numFalse == 1 && val != -1)
        {
            // b.grid[row, col] = val;
            return true;
        }
            
        return false;
    }
    public static Board Generate(Difficulty difficulty)
    {
        // mran.InitState();
        //update our thing.
        Board board = new Board(0);
        //Create our grid and everything.
        board.Init(difficulty.size, difficulty.gameType);
        board.grid = LatinSquare.CreateLatinSquare(difficulty.size);
        //Create all Comparisons
        board.FillAllComparisons();
        
        

        // //Remove until we reach ratio.
        // ReduceComparisons(difficulty); //0.2 means we want, of all possible comparisons, about 20% of them remaining.
        //
        // //Remove hints until we reach ratio.
        // ReduceValues(difficulty);
        
        
        //lets do them at the same time?
        if (board.gameType == GameType.Futoshiki)
        {
            board.ReduceBoth(difficulty);
        }
        else if(board.gameType == GameType.Renzoku)
        {
            //We dont get rid of comparisons in Renzoku because no comparison IS one of the comparisons :p
            board.ReduceValues(difficulty);
            //We dont do anything to display these, so lets clean our list up to improve performance.
            //CullNonAdjacentComparisons();
        }

        board.SetGivenFromCurrentGrid();
        board.original = board.LevelToString();
        return board;
    }

    private void FillElifibleComparisonVector()
    {
        //Fill eligibleComparisons list, used during number removal.
        eligibleComparisons = new List<Comparison>();
        foreach (Comparison c in comparisons)
        {
            eligibleComparisons.Add(c);
        }
    }
    private void FillEligibleNumberVector()
    {
        eligibleValues = new List<ValRef>();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (grid[i, j] != 0)//oops lol. silly big gave infinite loop, kept removing 0's and replacing them with 0's.
                {
                    eligibleValues.Add(new ValRef(i, j, grid[i, j]));
                }
            }
        }
    }

    public void FillRenzokuComparisons()
    {
        //Assume that All "Adjacent" Comparisons exist. This function is used when loading from a text file.
        
        //Horizontal
        for (int i = 0; i < size-1; i++)
        {
            for (int j = 0; j < size; j++)
            {
                (int, int) pos1 = (i, j);
                (int, int) pos2 = (i + 1, j);
                bool skip = false;
                foreach (Comparison c in comparisons)
                {
                    if ((c.pos1 == pos1 && c.pos2 == pos2) || (c.pos1 == pos2 && c.pos2 == pos1))
                    {
                        skip = true;
                        break;
                    }
                }
    
                if (!skip)
                {
                    AddComparator(pos1, pos2, Comparator.NonAdjacent);
                }
            }
        }
        //Vertical
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size-1; j++)
            {
                (int, int) pos1 = (i, j);
                (int, int) pos2 = (i , j+1);
                bool skip = false;
                foreach (Comparison c in comparisons)
                {
                    if ((c.pos1 == pos1 && c.pos2 == pos2) || (c.pos1 == pos2 && c.pos2 == pos1))
                    {
                        skip = true;
                        break;
                    }
                }
    
                if (!skip)
                {
                    AddComparator(pos1, pos2, Comparator.NonAdjacent);
                }            }
        }
    }
    public void FillAllComparisons()
    {
        comparisons = new List<Comparison>();
        compLookupTable = new Dictionary<((int, int), (int, int)), Comparison>();
        // bool isEmpty = true;
        //Horizontal
        for (int i = 0; i < size-1; i++)
        {
            for (int j = 0; j < size; j++)
            {
                (int, int) pos1 = (i, j);
                (int, int) pos2 = (i + 1, j);
                Comparison c = AddComparator(pos1,pos2,GetComparator(grid[i,j], grid[i+1,j]));
                compLookupTable.Add((pos1, pos2), c);
            }
        }
        //Vertical
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size-1; j++)
            {
                (int, int) pos1 = (i, j);
                (int, int) pos2 = (i , j+1);
                Comparison c = AddComparator(pos1,pos2,GetComparator(grid[i,j], grid[i,j+1]));
                compLookupTable.Add((pos1,pos2),c);
            }
        }
    }

    public Comparator GetComparator(int a, int b)
    {
        if (gameType == GameType.Futoshiki)
        {
            if (a > b)
            {
                return Comparator.GreaterThan;
            }

            if (a < b)
            {
                return Comparator.LessThan;
            }
        }
        else if(gameType == GameType.Renzoku)
        {
            if (Adjacent(a,b))
            {
                return Comparator.Adjacent;
            }
            else
            {
                return Comparator.NonAdjacent;
            }
        }
        
        
        if (a == b)
        {
            //This is an error, it should never happen in a latin square.
            return Comparator.Equal;
        }
        
        
        //also, it should be the only option after the above logic.
        return Comparator.Equal;
    }

    void CullNonAdjacentComparisons()
    {
        for (int i = comparisons.Count - 1;i>0; i--)
        {
            if (comparisons[i].comparator == Comparator.NonAdjacent)
            {
                comparisons.RemoveAt(i);
            }
        }
    }
    void ReduceComparisons(Difficulty difficulty)
    {
        // Remove numbers until this count has been reached
        int buffer = 0;
        int count = comparisons.Count;
        while (count > ((size*size)*difficulty.comparisonFactor + 1) && buffer < difficulty.numberRemovalMaxBuffer) {
            if (!FindComparisonToRemove())
            {
                buffer++;//failed removals.
            }
            else
            {
                buffer = 0;//reset the buffer, we need to fail x times in a row.
            }

            count = comparisons.Count;
        }
    }
    void ReduceValues(Difficulty difficulty)
    {
        int buffer = 0;
        int count = GridCount();
        float minVal = size * size * difficulty.numberFactor + 1;
        bool gotNumbers = false;
        while (buffer < difficulty.numberRemovalMaxBuffer && (!gotNumbers))
        {
            bool foundNumber = false;
            if (!gotNumbers)
            {
                foundNumber = FindValueToRemove();
            }


            if (foundNumber)
            {
                buffer = 0;
            }
            else
            {
                buffer++;
            }

            count = GridCount();
            gotNumbers = count < minVal;
            
            if (gotNumbers)
            {
                break;
            }
        }
    }

    void ReduceBoth(Difficulty difficulty)
    {
        int buffer = 0;
        int count = GridCount();
        float minComp = (size * (size-1) * 2) * difficulty.comparisonFactor + 1;
        float minVal = size * size * difficulty.numberFactor + 1;
        bool gotNumbers = false;
        bool gotComps = false;
        if (gameType == GameType.Renzoku)
        {
            gotComps = true;//skip getting rid of comprarisons entirely for renzoku.
        }
        while (buffer < difficulty.numberRemovalMaxBuffer && (!gotComps || !gotNumbers))
        {
            bool foundNumber = false;
            bool foundComp = false;
            if (!gotNumbers)
            {
                foundNumber = FindValueToRemove();
            }

            if (!gotComps)
            {
                foundComp = FindComparisonToRemove();
            }

            if (foundNumber || foundComp)
            {
                buffer = 0;
            }
            else
            {
                buffer++;
            }

            count = GridCount();
            gotComps = comparisons.Count < minComp;
            gotNumbers = count < minVal;
            
            if (gotComps && gotNumbers)
            {
                break;
            }
        }
    }
    private int GridCount()
    {
        int count = 0;
        foreach (int v in grid)
        {
            if (v != 0)
            {
                count++;
            }
        }

        return count;
    }

    private void SetGivenFromCurrentGrid()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (grid[i, j] == 0)
                {
                    givenValues[i,j] = false;
                    validSpaceCheck[i, j] = true;
                }
                else
                {
                    givenValues[i, j] = true;
                    validSpaceCheck[i, j] = true;
                }
            }
        }
    }
    public bool IsSolved()
    {
        //NOTE: refactored isValid to fail if it see's a 0.
        // if (!AnyEmpty())
        // {
        //     return false;
        // }
        
        return isValid();
    }
    private bool AnyEmpty()
    {
        //dont need to do a full count each time, we can stop once we find one empty.
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (grid[i, j] == 0)
                {
                    return false;
                }

            }
        }

        return true;
    }
    
    bool FindComparisonToRemove() {
        FillElifibleComparisonVector();
        bool compFound = false;

        // If no more candidates, bring back the previously removed number
        while (eligibleComparisons.Count > 0 && !compFound) {
            int random = mran.Next(0, eligibleComparisons.Count);
            // int random = UnityEngine.Random.Range(0, eligibleComparisons.Count);
            Comparison attempt = eligibleComparisons[random];
            
            eligibleComparisons.Remove(attempt);

            comparisons.Remove(attempt);
                // If unsolvable, bring number back and look for another
            if (!TrySolve())//if(!TrySolve())
            {
               
                comparisons.Add(attempt);
            } else {
                // // int check = 0;
                // int check = 0;
                // CountSolnLimOne(this,ref check);
                // if (check == 1)
                // {
                    compFound = true;
                // }
            }
        }

        if (!compFound ){
            return false;
        }

        return true;
    }
    bool FindValueToRemove() {
        FillEligibleNumberVector();
        bool valFound = false;

        // If no more candidates, bring back the previously removed number
        while (eligibleValues.Count > 0 && !valFound) {
            int random = mran.Next(0, eligibleValues.Count);
            // int random = UnityEngine.Random.Range(0, eligibleValues.Count);

            // int random = Random.Range(0,eligibleValues.Count);
            ValRef attempt = eligibleValues[random];
            eligibleValues.Remove(attempt);
            grid[attempt.x,attempt.y] = 0;
            // If unsolvable, bring number back and look for another
            if (!TrySolve())//TrySOlve
            {
                grid[attempt.x,attempt.y] = attempt.val;
            } else
            {
                // int check = 0;
                // CountSolnLimOne(this,ref check);
                // if (check == 1)
                // {
                //     valFound = true;
                // }
                valFound = true;
            }
        }
        
        return valFound;
    }

    public static bool Adjacent(int a, int b)
    {
        return (int)Mathf.Abs(a - b) == 1;
    }
    static string alphabet = "#abcdefghijklmnopqrstuvwxyz";
    public static int CharToInt(char gap)
    {
        gap = Char.ToLower(gap);
        return alphabet.IndexOf(gap);
    }
    public static string IntToString(int i)
    {
        if (i == 0)
        {
            return "";
        }
        if (i < 0)
        {
            GD.PrintErr("cant turn negative number into string representation, dingus");
        }
        if (i <= 24)
        {
            return alphabet[i].ToString();
        }

        return "z" + IntToString(i - 24); //error
    }

    public static string ComparatorToString(Comparator c, bool horiz)
    {
        if (horiz)
        {
            if (c == Comparator.GreaterThan)
            {
                return ">";
            }
            else if (c == Comparator.LessThan)
            {
                return "<";
            }
            else if (c == Comparator.Adjacent)
            {
                return ".";
            }
        }else{
        
            if (c == Comparator.GreaterThan)
            {
                return  "v";
            }else if (c == Comparator.LessThan)
            {
                return  "^";
            }else if (c == Comparator.Adjacent)
            {
                return  "*";
            }
        }

        return "";
    }
    public static (int, int) GetPosFromCount(int count, int size)
    {
        return ( (count % size),(int) size - (count / size) - 1);
    }

    public static int BitArrayToInt(BitArray ba)
    {
        if (ba.Length > 32)
        {
            GD.PrintErr("bit array too large to convert to int.");
        }
        
        int[] array = new int[1];
        ba.CopyTo(array, 0);
        return array[0];
    }

   
    
    public static int Solve(Board board, ref int solutions, bool stopAtOne = false,bool returnAfterOne=true)
    {
        int row = -1; 
        int col = -1;
        
        bool isEmpty = true; 
        for (int i = 0; i < board.size; i++) { 
            for (int j = 0; j < board.size; j++) { 
                if (board.grid[i, j] == 0) { 
                    row = i; 
                    col = j; 
  
                    // we still have some remaining 
                    // missing values in Sudoku 
                    isEmpty = false; 
                    break; 
                } 
            } 
            if (!isEmpty) { 
                break; 
            } 
        } 
  
        //
        
        // no empty space left 
        if (isEmpty) {
            //return true, solved!
            solutions++;
            return solutions; 
        } 
  
        // else for each-row backtrack 
        for (int num = 1; num <= board.size; num++) { 
            if (IsSafe(board, row, col, num)) { 
                board.grid[row, col] = num; 
                if (Solve(board, ref solutions) > 0)
                {
                    //solutions++;
                    // print(board, n);
                    //return solutions
                    // return solutions;
                    if (stopAtOne)
                    {
                        return solutions;
                    }

                    if (returnAfterOne)
                    {
                        if (solutions > 1)
                        {
                            return solutions;//we can end our search early if we are only looking for one unique solution. The second we find a second solution, thats game over.
                        }
                    }
                    //keep going? More than one unique solution, so lets undo and keep searching.
                    board.grid[row, col] = 0;
                } 
                else
                {
                    // replace it 
                    board.grid[row, col] = 0; 
                } 
            } 
        } 
        //return false
        return solutions; 
    }
    
    // START: Check if the grid is uniquely solvable
    static void CountSoln(Board board, ref int number)
    {
        int row, col;

        if(!FindUnassignedLocation(board, out row, out col))
        {
            number++;
            return ;
        }
        
        for(int i=0;i<board.size && number<2;i++)
        {
            if( IsSafe(board, row, col, i) )//ALGORITHM USED RANDOM NUMBER ORDER
            {
                board.grid[row,col] = i;//ALGORITHM USED RANDOM NUMBER ORDER
                CountSoln(board, ref number);
            }

            board.grid[row,col] = 0;
        }

    }
    static void CountSolnLimOne(Board board, ref int number)
    {
        int row, col;

        if(!FindUnassignedLocation(board, out row, out col))
        {
            number++;
            return ;
        }

        if (number > 1)
        {
            return;//we dont need to find more than 2 unique solutiuons.
        }
        
        for(int i=0;i<board.size && number<2;i++)
        {
            if( IsSafe(board, row, col, i) )//ALGORITHM USED RANDOM NUMBER ORDER
            {
                board.grid[row,col] = i;//ALGORITHM USED RANDOM NUMBER ORDER
                CountSoln(board, ref number);
            }

            board.grid[row,col] = 0;
        }

    }
// END: Check if the grid is uniquely solvable
    private static bool IsSafe(Board board, int row, int col, int num)
    {

        if (UsedInRow(board, row, num))
        {
            return false;
        }

        if (UsedInCol(board, col, num))
        {
            return false;
        }
        
        foreach (Comparison c in board.comparisons)
        {
            if (c.pos1 == (row, col))
            {
                if (c.comparator == Comparator.GreaterThan)
                {
                    if (num == 1)
                    {
                        return false;
                    }

                    if (board.grid[c.pos2.Item1, c.pos2.Item2] != 0)
                    {
                        if (num <= board.grid[c.pos2.Item1, c.pos2.Item2])
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.LessThan)
                {
                    if (num == board.size)
                    {
                        return false;
                    }
                    
                    if (board.grid[c.pos2.Item1, c.pos2.Item2] != 0)
                    {
                        if (num >= board.grid[c.pos2.Item1, c.pos2.Item2])
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.Adjacent)
                {
                    if (board.grid[c.pos2.Item1, c.pos2.Item2] != 0)
                    {
                        if (!Adjacent(num, board.grid[c.pos2.Item1, c.pos2.Item2]))
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.NonAdjacent)
                {
                    if (board.grid[c.pos2.Item1, c.pos2.Item2] != 0)
                    {
                        if (Adjacent(num, board.grid[c.pos2.Item1, c.pos2.Item2]))
                        {
                            return false;
                        }
                    }
                }
            }
            if (c.pos2 == (row, col))
            {
                if (c.comparator == Comparator.GreaterThan)
                {//num<a
                    if (num == 4)
                    {
                        return false;
                    }

                    if (board.grid[c.pos1.Item1, c.pos1.Item2] != 0)
                    {
                        if (num >= board.grid[c.pos1.Item1, c.pos1.Item2])
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.LessThan)
                {//num>a
                    if (num == board.size)
                    {
                        return false;
                    }
                    
                    if (board.grid[c.pos1.Item1, c.pos1.Item2] != 0)
                    {
                        if (num <= board.grid[c.pos1.Item1, c.pos1.Item2])
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.Adjacent)
                {
                    if (board.grid[c.pos1.Item1, c.pos1.Item2] != 0)
                    {
                        if (!Adjacent(num, board.grid[c.pos1.Item1, c.pos1.Item2]))
                        {
                            return false;
                        }
                    }
                }
                else if (c.comparator == Comparator.NonAdjacent)
                {
                    if (board.grid[c.pos1.Item1, c.pos1.Item2] != 0)
                    {
                        if (Adjacent(num, board.grid[c.pos1.Item1, c.pos1.Item2]))
                        {
                            return false;
                        }
                    }
                }
            }

        }

        return true;
    }
    private static bool UsedInRow(Board board, int row, int val)
    {
        for(int col = 0;col<board.size;col++)
        {
            if (board.grid[row, col] == val)
            {
                return true;
            }
        }

        return false;
    }
    private static bool UsedInCol(Board board, int col, int val)
    {
        for(int row = 0;row<board.size;row++)
        {
            if (board.grid[row, col] == val)
            {
                return true;
            }
        }
        return false;
    }
    private static bool FindUnassignedLocation(Board board, out int row, out int col)
    { 
        row = 0;
        col = 0;
        for (row = 0; row < board.size; row++)
        {
            for (col = 0; col < board.size; col++)
            {
                if (board.grid[row,col] == 0)
                    return true;
            }
        }
        return false;
    }
    public static BitArray IntToBitArray(int bi, int size)
    {
        // BitArray ba = new BitArray(size);
        // for (int i = 0; i < size; i++)
        // {
        //     ba.Set(i,(bi << i) == 1);
        // }
        BitArray ba = new BitArray(new[] { bi });
        return ba;
    }
}

