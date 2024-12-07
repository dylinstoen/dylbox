using System;
using System.Collections.Generic;
[Serializable]
public class GameState
{
	public string status;      // "lobby", "in_game", etc.
	public int round;
	public int maxRounds;
	public Dictionary<string, int> scores;
}
