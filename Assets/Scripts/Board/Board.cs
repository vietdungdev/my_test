﻿using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private Cell[,] m_cells;

    private Transform m_root;

    private int m_matchMin;

    private ItemSkins m_itemSkins;

    public Board(Transform transform, GameSettings gameSettings, ItemSkins itemSkins)
    {
        m_root = transform;

        m_itemSkins = itemSkins;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];

        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

    }

    internal void Fill()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();

                List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
                if (cell.NeighbourBottom != null)
                {
                    NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                if (cell.NeighbourLeft != null)
                {
                    NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                item.SetType(Utils.GetRandomNormalTypeExcept(types.ToArray()));
                item.SetView(m_itemSkins);
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(false);
            }
        }
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                list.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                m_cells[x, y].Assign(list[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }

    private List<Cell> _FillCell = new List<Cell>();
    private int[] normalItemCount = new int[7];
    internal void FillGapsWithNewItems()
    {
        //Debug.Log($"---------FillGapsWithNewItems---------");
        _FillCell.Clear();
        for (int i = 0; i < normalItemCount.Length; i++)
        {
            normalItemCount[i] = 0;
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                    _FillCell.Add(cell);
                else
                {
                    NormalItem nitem = cell.Item as NormalItem;
                    if (nitem != null)
                    {
                        normalItemCount[(int)nitem.ItemType]++;
                    }
                }
            }
        }

        int min = int.MaxValue;
        NormalItem.eNormalType minType = NormalItem.eNormalType.TYPE_ONE;
        for (int i = 0; i < normalItemCount.Length; i++)
        {
            if (normalItemCount[i] < min)
            {
                min = normalItemCount[i];
                minType = (NormalItem.eNormalType)i;
            }
        }

        foreach (var cell in _FillCell)
        {
            GetSurroundingItems(cell);
            _SurroundingItem.Add(minType);

            NormalItem item = new NormalItem();
            var itemType = Utils.GetRandomNormalTypeExcept(_SurroundingItem.ToArray());
            item.SetType(itemType);
            item.SetView(m_itemSkins);
            item.SetViewRoot(m_root);

            cell.Assign(item);
            cell.ApplyItemPosition(true);
        }
    }

    private HashSet<NormalItem.eNormalType> _SurroundingItem = new HashSet<NormalItem.eNormalType>();
    private void GetSurroundingItems(Cell cell)
    {
        _SurroundingItem.Clear();

        if (cell.NeighbourUp != null)
        {
            NormalItem nitem = cell.NeighbourUp.Item as NormalItem;
            if (nitem != null)
                _SurroundingItem.Add(nitem.ItemType);
        }

        if (cell.NeighbourBottom != null)
        {
            NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
            if (nitem != null)
                _SurroundingItem.Add(nitem.ItemType);
        }

        if (cell.NeighbourLeft != null)
        {
            NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
            if (nitem != null)
                _SurroundingItem.Add(nitem.ItemType);
        }

        if (cell.NeighbourRight != null)
        {
            NormalItem nitem = cell.NeighbourRight.Item as NormalItem;
            if (nitem != null)
                _SurroundingItem.Add(nitem.ItemType);
        }

    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        item.View.DOMove(cell2.transform.position, 0.3f);
        item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    }

    List<Cell> _HorizontalMatches = new List<Cell>();
    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        _HorizontalMatches.Clear();
        _HorizontalMatches.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _HorizontalMatches.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _HorizontalMatches.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return _HorizontalMatches;
    }

    List<Cell> _VerticalMatches = new List<Cell>();
    public List<Cell> GetVerticalMatches(Cell cell)
    {
        _VerticalMatches.Clear();
        _VerticalMatches.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _VerticalMatches.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _VerticalMatches.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return _VerticalMatches;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetView(m_itemSkins);
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    List<Cell> _FindFirstMatch = new List<Cell>();
    internal List<Cell> FindFirstMatch()
    {
        _FindFirstMatch.Clear();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    _FindFirstMatch.AddRange(listhor);
                    return _FindFirstMatch;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    _FindFirstMatch.AddRange(listvert);
                    return _FindFirstMatch;
                }
            }
        }

        return _FindFirstMatch;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if (bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    private List<Cell> _PotentialMatches = new List<Cell>();
    private HashSet<Cell> _CheckedCell = new HashSet<Cell>();
    internal List<Cell> GetPotentialMatches()
    {
        _PotentialMatches.Clear();
        _CheckedCell.Clear();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                if (_CheckedCell.Contains(cell))
                    continue;

                _CheckedCell.Add(cell);

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */
                if (cell.NeighbourRight != null)
                {
                    _CheckedCell.Add(cell.NeighbourRight);
                    _PotentialMatches.AddRange(GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight));
                    if (_PotentialMatches.Count > 0)
                        break;
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp != null)
                {
                    _CheckedCell.Add(cell.NeighbourUp);
                    _PotentialMatches.AddRange(GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp));
                    if (_PotentialMatches.Count > 0)
                        break;
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom != null)
                {
                    _CheckedCell.Add(cell.NeighbourBottom);
                    _PotentialMatches.AddRange(GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom));
                    if (_PotentialMatches.Count > 0)
                        break;
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft != null)
                {
                    _CheckedCell.Add(cell.NeighbourLeft);
                    _PotentialMatches.AddRange(GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft));
                    if (_PotentialMatches.Count > 0)
                        break;
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second != null)
                    {
                        _PotentialMatches.Add(cell);
                        _PotentialMatches.Add(neib.NeighbourRight);
                        _PotentialMatches.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = cell.NeighbourUp;
                if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second != null)
                    {
                        _PotentialMatches.Add(cell);
                        _PotentialMatches.Add(neib.NeighbourUp);
                        _PotentialMatches.Add(second);
                        break;
                    }
                }
            }

            if (_PotentialMatches.Count > 0)
                break;
        }

        return _PotentialMatches;
    }

    List<Cell> _PotentialMatch = new List<Cell>();
    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        _PotentialMatch.Clear();
        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                _PotentialMatch.Add(cell);
                _PotentialMatch.Add(neighbour);
                _PotentialMatch.Add(third);
            }
        }

        return _PotentialMatch;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }
}
