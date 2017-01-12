using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Test : MonoBehaviour
{
	void Start ()
    {
        List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>> indices = new List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>>();
        Mahjong.Rule rule = new Mahjong.Rule();
        Debug.Log(rule.Check(new Mahjong.Rule.Enumerator(new LinkedList<LinkedListNode<int>>(new LinkedListNode<int>[]
        {
            //new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 2) << 2),//
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 2) << 2),//

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2),//
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2),//

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 4) << 2),//
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 4) << 2),
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 5) << 2),//

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 5) << 2),
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2),
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2),

        })), delegate(int index, IEnumerable<Mahjong.Rule.WinFlag> winFlags)
        {
            if (indices == null)
                indices = new List<KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>>();

            indices.Add(new KeyValuePair<int, IEnumerable<Mahjong.Rule.WinFlag>>(index, winFlags));
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
