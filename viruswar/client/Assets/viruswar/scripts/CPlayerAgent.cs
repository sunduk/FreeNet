using System.Collections.Generic;
using System.Collections;

public struct CellInfo
{
	public int score;
	public short from_cell;
	public short to_cell;
}

public class CPlayerAgent
{
	public CellInfo run(List<short> board, List<CPlayer> players, List<short> attacker_cells, List<short> victim_cells)
	{
		List<CellInfo> cell_scores = new List<CellInfo>();
		int total_best_score = 0;
		attacker_cells.ForEach(cell => 
		{
			int best_score = 0;
			short cell_the_best = 0;
			List<short> available_cells = CHelper.find_available_cells(cell, board, players);
			available_cells.ForEach(to_cell => 
			{
				// simulate!
				int score = calc_score(cell, to_cell, victim_cells);
				if (best_score < score)
				{
					cell_the_best = to_cell;
					best_score = score;
				}
			});
			
			if (total_best_score < best_score)
			{
				total_best_score = best_score;
			}
			
			CellInfo info = new CellInfo();
			info.score = best_score;
			info.from_cell = cell;
			info.to_cell = cell_the_best;
			cell_scores.Add(info);
		});
		
		List<CellInfo> top_scores = cell_scores.FindAll(info => info.score == total_best_score);
		System.Random rnd = new System.Random();
		int index = rnd.Next(0, top_scores.Count);
		return top_scores[index];
		
		//cell_scores.Sort(delegate(CellInfo left, CellInfo right) { return right.score.CompareTo(left.score); });
		//return cell_scores[0];
	}
	
	int calc_score(short from_cell, short to_cell, List<short> victim_cells)
	{
		int score = 0;
		
		// 1. Calculate move score. clone = 1, move = 0
		short distance = CHelper.get_distance(from_cell, to_cell);
		if (distance <= 1)
		{
			score = 1;
		}
		
		// 2. Calculate fighting score.
		int fighting_score = calc_cellcount_to_eat(to_cell, victim_cells);
		
		return score + fighting_score;
	}
	
	int calc_cellcount_to_eat(short cell, List<short> victim_cells)
	{
		List<short> cells_to_eat = CHelper.find_neighbor_cells(cell, victim_cells, 1);
		return cells_to_eat.Count;
	}
}
