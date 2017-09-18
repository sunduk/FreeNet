using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CHelper
{
	// Find cells to movable.
	public static List<short> find_available_cells(short basis_cell, List<short> total_cells, List<CPlayer> players)
	{
		List<short> targets = find_neighbor_cells(basis_cell, total_cells, 2);

		players.ForEach(obj => {
			targets.RemoveAll(number => obj.cell_indexes.Exists(cell => cell == number));
		});
		
		//string debug = basis_cell.ToString() + " => ";
		//targets.ForEach(obj => {
		//	debug += string.Format ("{0}, ", obj);
		//});
		//Debug.Log(debug);
		
		return targets;
	}
	
	// Find neighbor cells of this cell.
	// targets : total game board.
	public static List<short> find_neighbor_cells(short basis_cell, List<short> targets, short gap)
	{
		Vector2 pos = convert_to_position(basis_cell);
        return targets.FindAll(obj => get_distance(pos, convert_to_position(obj)) <= gap);
	}

	// calculate vertical index
	public static short calc_row(short cell)
	{
		return (short)(cell / CBattleRoom.COL_COUNT);
	}
	
	// calculate horizontal index
	public static short calc_col(short cell)
	{
		return (short)(cell % CBattleRoom.COL_COUNT);
	}
	
	// convert cell number to (x,y) position.
	public static Vector2 convert_to_position(short cell)
	{
		return new Vector2(calc_col(cell), calc_row(cell));
	}
	
	public static byte get_distance(short from, short to)
	{
		return get_distance(convert_to_position(from), convert_to_position(to));
	}
	
	public static byte get_distance(Vector2 pos1, Vector2 pos2)
	{
		Vector2 distance = (pos1 - pos2);
		short x = (short)Mathf.Abs(distance.x);
		short y = (short)Mathf.Abs(distance.y);
		return (byte)Mathf.Max(x,y);
	}
	
	public static byte howfar_from_clicked_cell(short basis_cell, short cell)
	{
		short row = (short)(basis_cell / CBattleRoom.COL_COUNT);
		short col = (short)(basis_cell % CBattleRoom.COL_COUNT);
		Vector2 basic_pos = new Vector2(col, row);
		
		row = (short)(cell / CBattleRoom.COL_COUNT);
		col = (short)(cell % CBattleRoom.COL_COUNT);
		Vector2 cell_pos = new Vector2(col, row);
		
		Vector2 distance = (basic_pos - cell_pos);
		short x = (short)Mathf.Abs(distance.x);
		short y = (short)Mathf.Abs(distance.y);
		return (byte)Mathf.Max(x,y);
	}
	
	public static bool can_play_more(List<short> board, List<CPlayer> players, int current_player_index)
	{
		CPlayer current = players[current_player_index];
		foreach(byte cell in current.cell_indexes)
		{
			if (CHelper.find_available_cells(cell, board, players).Count > 0)
			{
				return true;
			}
		}
		return false;
	}

    public static Vector3 map_to_world(Vector2 map_position)
    {
        Vector3 world = new Vector3(
            map_position.x * 30.0f,
            map_position.y * -30.0f,
            0.0f);

        world += new Vector3(-90.0f, 90.0f, 0.0f);
        return world;
    }
}
