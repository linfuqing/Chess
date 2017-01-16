using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Test : MonoBehaviour
{
	void Start () 
    {
        List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>> indices = new List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>>();
        Mahjong.Rule rule = new Mahjong.Rule();
        Debug.Log(rule.Check(new Mahjong.Player.Enumerator(new LinkedList<int>(new int[]
        {
            //new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),
            new Mahjong.Tile(Mahjong.TileType.Bamboo, 0) << 2,//0
            new Mahjong.Tile(Mahjong.TileType.Bamboo, 0) << 2,//1

            new Mahjong.Tile(Mahjong.TileType.Bamboo, 5) << 2,//2
            new Mahjong.Tile(Mahjong.TileType.Bamboo, 5) << 2,//3

        })), rule.winFlags, delegate(bool isEye, int count, int index, IEnumerable<Mahjong.Rule.WinFlag> winFlags)
        {
            string temp = isEye.ToString() + ':' + count + ':' + index;
            foreach (Mahjong.Rule.WinFlag winFlag in winFlags)
                temp += ", " + winFlag.index;

            Debug.Log(temp);

            return true;
            /*if (indices == null)
                indices = new List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>>();

            indices.Add(new KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>(index, winFlags));*/
        }));
        
        if(indices != null)
        {
            foreach (KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>> pair in indices)
            {
                string temp = pair.Key.ToString();
                foreach (Mahjong.Rule.WinFlag winFlag in pair.Value)
                    temp += ", " + winFlag.index;

                Debug.Log(temp);
            }
        }
    }
	
}
