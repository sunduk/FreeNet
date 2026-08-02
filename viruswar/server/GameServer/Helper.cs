using System.Numerics;

namespace GameServer;

public static class Helper
{
    private static readonly byte ColumnCount = 7;

    /// <summary>
    /// Gets the column index from a position.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static short CalcColumn(short position) => (short)(position % ColumnCount);

    /// <summary>
    /// Gets the row index from a position.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static short CalcRow(short position) => (short)(position / ColumnCount);

    /// <summary>
    /// Checks whether the game can continue.
    /// </summary>
    /// <param name="board">The list of all cells on the board.</param>
    /// <param name="currentPlayer">The current player.</param>
    /// <param name="allPlayer">The list of all players.</param>
    /// <returns>True if the game can continue, otherwise false.</returns>
    public static bool CanPlayMore(List<short> board, Player currentPlayer, List<Player> allPlayer)
    {
        foreach (var cell in currentPlayer.Viruses)
        {
            if (Helper.FindAvailableCells(cell, board, allPlayer).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds movable cells and returns them as a list.
    /// </summary>
    /// <param name="basisCell">The basis cell to find available cells for.</param>
    /// <param name="totalCells">The list of all cells on the board.</param>
    /// <param name="players">The list of all players.</param>
    /// <returns>A list of available cells that the basis cell can move to.</returns>
    public static List<short> FindAvailableCells(short basisCell, List<short> totalCells, List<Player> players)
    {
        var targets = FindNeighborCells(basisCell, totalCells, 2);

        foreach (var player in players)
        {
            _ = targets.RemoveAll(number => player.Viruses.Exists(cell => cell == number));
        }

        return targets;
    }

    /// <summary>
    /// Finds nearby cell positions and returns them as a list.
    /// </summary>
    /// <param name="basisCell">The basis cell to find neighbors for.</param>
    /// <param name="targets">The list of target cells to consider.</param>
    /// <param name="gap">The maximum distance to consider a cell a neighbor.</param>
    /// <returns>A list of neighboring cells within the specified gap.</returns>
    public static List<short> FindNeighborCells(short basisCell, List<short> targets, short gap)
    {
        var pos = ConvertToXY(basisCell);
        return targets.FindAll(obj => GetDistance(pos, ConvertToXY(obj)) <= gap);
    }

    /// <summary>
    /// Returns distance between two cell indices. One-cell gap = 1, two-cell gap = 2.
    /// </summary>
    /// <param name="from">The starting position.</param>
    /// <param name="to">The ending position.</param>
    /// <returns>The distance between the two positions.</returns>
    public static short GetDistance(short from, short to)
    {
        var pos1 = ConvertToXY(from);
        var pos2 = ConvertToXY(to);
        return GetDistance(pos1, pos2);
    }

    /// <summary>
    /// Gets the distance.
    /// </summary>
    /// <param name="first">The first position.</param>
    /// <param name="second">The second position.</param>
    /// <returns>System.Int16.</returns>
    public static short GetDistance(Vector2 first, Vector2 second)
    {
        var distance = first - second;

        var x = (short)Math.Abs(distance.X);
        var y = (short)Math.Abs(distance.Y);

        // The larger of x and y represents the actual distance between the two positions.
        return Math.Max(x, y);
    }

    /// <summary>
    /// Converts (row, col) coordinates to a position.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <returns>The position corresponding to the specified row and column.</returns>
    public static short GetPosition(byte row, byte column) => (short)((row * ColumnCount) + column);

    public static byte HowFarFromClickedCell(short basisCell, short cell)
    {
        var row = (short)(basisCell / ColumnCount);
        var col = (short)(basisCell % ColumnCount);
        var basicPos = new Vector2(col, row);

        row = (short)(cell / ColumnCount);
        col = (short)(cell % ColumnCount);
        var cellPos = new Vector2(col, row);

        var distance = basicPos - cellPos;
        var x = (short)Math.Abs(distance.X);
        var y = (short)Math.Abs(distance.Y);
        return (byte)Math.Max(x, y);
    }

    /// <summary>
    /// Shuffles the specified list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list.</param>
    public static void Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    /// <summary>
    /// Converts a position to (row, col) coordinates.
    /// </summary>
    /// <param name="cell">The position to convert.</param>
    /// <returns>The corresponding (row, col) coordinates.</returns>
    private static Vector2 ConvertToXY(short position) => new(CalcColumn(position), CalcRow(position));
}
