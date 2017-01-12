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
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//0
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//1

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 1) << 2),//2
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 2) << 2),//3
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 2) << 2),//4

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2),//5
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2),//6

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 4) << 2),//7
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 4) << 2),//8
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 5) << 2),//9

            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 5) << 2),//10
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2),//11
            new LinkedListNode<int>(new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2),//12

        })), delegate(bool isEye, int count, int index, IEnumerable<Mahjong.Rule.WinFlag> winFlags)
        {
            string temp = isEye.ToString() + ':' + count + ':' + index;
            foreach (Mahjong.Rule.WinFlag winFlag in winFlags)
                temp += ", " + winFlag.index;

            Debug.Log(temp);
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
