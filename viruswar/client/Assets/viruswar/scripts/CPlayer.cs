using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PLAYER_STATE
{
	HUMAN,
	AI
}

public class CPlayer : MonoBehaviour {
	
	public List<short> cell_indexes { get; private set; }
	public byte player_index { get; private set; }
	public PLAYER_STATE state { get; private set; }
	CPlayerAgent agent;
	
	void Awake()
	{
		this.cell_indexes = new List<short>();
		this.agent = new CPlayerAgent();
	}
	
	
	public void clear()
	{
		this.cell_indexes.Clear();
	}
	
	public void initialize(byte player_index)
	{
		this.player_index = player_index;
	}
	
	public void add(short cell)
	{
		if (this.cell_indexes.Contains(cell))
		{
			Debug.LogError(string.Format("Already have a cell. {0}", cell));
			return;
		}
		
		this.cell_indexes.Add(cell);
	}
	
	public void remove(short cell)
	{
		this.cell_indexes.Remove(cell);
	}
	
	public void change_to_agent()
	{
		this.state = PLAYER_STATE.AI;
	}
	
	public void change_to_human()
	{
		this.state = PLAYER_STATE.HUMAN;
	}
	
	public CellInfo run_agent(List<short> board, List<CPlayer> players, List<short> victim_cells)
	{
		return this.agent.run(board, players, this.cell_indexes, victim_cells);
	}

	public int get_virus_count()
	{
		return this.cell_indexes.Count;
	}
}
