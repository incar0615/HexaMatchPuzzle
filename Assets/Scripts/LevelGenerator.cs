using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapInfo
{
    public Vector2Int mapSize;

    public string mapInfoStr;
}
public class LevelGenerator : MonoBehaviour
{
    public int width = 7;
    public int height = 11;

    public Vector2Int spawnPoint;

    bool isMovedTick;

    public GameObject bgBlockPrefap;
    public GameObject hexaBlockPrefap;

    [HideInInspector]
    public List<GameObject> bgBlocks;
    [HideInInspector]
    public List<HexaBlock> hexaBlocks;

    public BlockType[,] blockBoard;

    private static LevelGenerator instance = null;
    public static LevelGenerator Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType<LevelGenerator>();
                if (!instance) instance = new GameObject("LevelGenerator").AddComponent<LevelGenerator>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        bgBlocks = new List<GameObject>();
        hexaBlocks = new List<HexaBlock>();

        MapInfo mapInfo = new MapInfo();

        spawnPoint = new Vector2Int(3, 0);

        mapInfo.mapSize = new Vector2Int(width, height);
        mapInfo.mapInfoStr =
            "0,0,2,1,2,0,0," +
            "1,2,1,1,1,2,1," +
            "1,1,1,1,1,1,1," +
            "1,1,1,2,1,1,1," +
            "0,2,2,1,2,2,0," +
            "0,0,0,2,0,0,0";

        Generate(mapInfo);
    }

    public void Generate(MapInfo mapInfo)
    {
        int xSize = mapInfo.mapSize.x;
        int ySize = mapInfo.mapSize.y;

        blockBoard = new BlockType[xSize, ySize];

        string[] blockInfos;
        blockInfos = mapInfo.mapInfoStr.Split(',');

        // 적합성 체크
        if (blockInfos.Length != xSize * ySize)
        {
            // 잘못된 맵 정보 
            Debug.Log("MapInfo Error -----------" + blockInfos.Length + "!=" + xSize * ySize);
            return;
        }

        GameObject bgBlockParent = GameObject.Find("BgBlocks");

        // 블록 생성 
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                int blockNum = x + y * xSize;

                if(blockInfos[blockNum] == "0")
                {
                    blockBoard[x, y] = BlockType.EMPTY;
                    continue;
                }

                float yPosModifier = 0;
                if (x % 2 == 0) yPosModifier = -0.5f;
                
                bgBlocks.Add(Instantiate(bgBlockPrefap, new Vector3(x * 0.75f, -y + yPosModifier, 0), Quaternion.identity, bgBlockParent.transform));

                HexaBlock block = Instantiate(hexaBlockPrefap, new Vector3(x * 0.75f, -y + yPosModifier, 0), Quaternion.identity, transform).GetComponent<HexaBlock>();
                block.SetBlockStatus((BlockStatus)int.Parse(blockInfos[blockNum]), x, y);

                hexaBlocks.Add(block);

                blockBoard[x, y] = block.blockType;
            }
        }
    }

    public BlockType GetBlockType(int xPos, int yPos)
    {
        if (xPos < 0 || xPos > width - 1 || yPos < 0 || yPos > height - 1)
        {
            return BlockType.EMPTY;
        }

        return blockBoard[xPos, yPos];
    }

    public HexaBlock GetBlock(int xPos, int yPos)
    {
        return hexaBlocks.Find(block => block.xPos == xPos && block.yPos == yPos);
    }

    public void SwapBlocks(HexaBlock block0, HexaBlock block1)
    {
        // BlockType Board Swap
        BlockType tempType = blockBoard[block0.xPos, block0.yPos];
        blockBoard[block0.xPos, block0.yPos] = blockBoard[block1.xPos, block1.yPos];
        blockBoard[block1.xPos, block1.yPos] = tempType;

        block0.blockType = blockBoard[block0.xPos, block0.yPos];
        block1.blockType = blockBoard[block1.xPos, block1.yPos];

        BlockStatus tempStatus = block0.blockStatus;
        block0.blockStatus = block1.blockStatus;
        block1.blockStatus = tempStatus;

        // sprite swap
        Sprite tempSpr = block0.sprRenderer.sprite;
        block0.sprRenderer.sprite = block1.sprRenderer.sprite;
        block1.sprRenderer.sprite = tempSpr;

        Color tempColor = block0.sprRenderer.color;
        block0.sprRenderer.color = block1.sprRenderer.color;
        block1.sprRenderer.color = tempColor;

        int tempHp = block0.hp;
        block0.hp = block1.hp;
        block1.hp = tempHp;
        
    }

    public bool MoveEmptyBlock(HexaBlock emptyBlock)
    {
        isMovedTick = false;

        HexaBlock upperBlock = GetBlock(emptyBlock.xPos, emptyBlock.yPos - 1);
        if (upperBlock && upperBlock.blockType != BlockType.EMPTY)
        {
            SwapBlocks(emptyBlock, upperBlock);
            isMovedTick = true;
            return true;
        }
        return false;
    }

    public bool MoveEmptySideBlock(HexaBlock emptyBlock)
    {
        if (isMovedTick) return false;

        bool isOddBlock = (emptyBlock.xPos % 2 == 1);
        HexaBlock upperRightBlock = isOddBlock ? GetBlock(emptyBlock.xPos + 1, emptyBlock.yPos - 1)
            : GetBlock(emptyBlock.xPos + 1, emptyBlock.yPos);
        if (upperRightBlock && upperRightBlock.blockType != BlockType.EMPTY)
        {
            SwapBlocks(emptyBlock, upperRightBlock);
            return true;
        }

        HexaBlock upperLeftBlock = isOddBlock ? GetBlock(emptyBlock.xPos - 1, emptyBlock.yPos - 1)
            : GetBlock(emptyBlock.xPos - 1, emptyBlock.yPos);
        if (upperLeftBlock && upperLeftBlock.blockType != BlockType.EMPTY)
        {
            SwapBlocks(emptyBlock, upperLeftBlock);
            return true;
        }
        return false;
    }

    public void SpawnGem()
    {
        HexaBlock spawnBlock = GetBlock(spawnPoint.x, spawnPoint.y);
        if(spawnBlock && spawnBlock.blockType == BlockType.EMPTY)
        {
            spawnBlock.SetBlockStatus(BlockStatus.BLOCK, spawnPoint.x, spawnPoint.y);
            blockBoard[spawnPoint.x, spawnPoint.y] = spawnBlock.blockType;
        }
    }
}
