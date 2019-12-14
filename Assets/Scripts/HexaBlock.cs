using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BlockStatus
{
    EMPTY = 0,
    BLOCK = 1,
    TOPS = 2,
}

public enum BlockType
{
    NONE = 0,
    EMPTY,
    OBSTACLE,
    RED,
    GREEN,
    BLUE,
    YELLOW,
    ORANGE,
    PURPLE,
}
public class HexaBlock : MonoBehaviour {

    public Sprite gemSprite;
    public Sprite[] topsSprite;
    public SpriteRenderer sprRenderer;

    public BlockType blockType;
    public BlockStatus blockStatus;

    public int xPos, yPos;

    public int hp;
    // Use this for initialization
    void Awake () {
        blockType = BlockType.OBSTACLE;

        sprRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    
    public void SetBlockStatus(BlockStatus blockInfo, int x, int y)
    {
        xPos = x;
        yPos = y;

        blockStatus = blockInfo;

        switch((BlockStatus)blockInfo)
        {
            case BlockStatus.BLOCK:
                sprRenderer.sprite = gemSprite;

                blockType = (BlockType)(Random.Range((int)BlockType.RED, (int)BlockType.PURPLE));
                switch(blockType)
                {
                    case BlockType.RED:
                        sprRenderer.color = new Color(1, 0, 0);
                        break;

                    case BlockType.GREEN:
                        sprRenderer.color = new Color(0, 1, 0);
                        break;

                    case BlockType.BLUE:
                        sprRenderer.color = new Color(0, 0, 1);
                        break;

                    case BlockType.YELLOW:
                        sprRenderer.color = new Color(1, 1, 0);
                        break;

                    case BlockType.ORANGE:
                        sprRenderer.color = new Color(1, 0.5f, 0);
                        break;

                    case BlockType.PURPLE:
                        sprRenderer.color = new Color(0.7f, 0, 1);
                        break;
                }

                break;

            case BlockStatus.TOPS:
                blockType = BlockType.OBSTACLE;
                sprRenderer.sprite = topsSprite[1];

                hp = 2;
                break;
        }
    }

    public void Matched(bool isGenerateLogic = false)
    {
        if(isGenerateLogic)
        {
            sprRenderer.sprite = null;
            sprRenderer.color = Color.white;
        }
        else
        {
            StartCoroutine(disapper());
        }
       
        blockType = BlockType.EMPTY;
        LevelGenerator.Instance.blockBoard[xPos, yPos] = BlockType.EMPTY;
    }

    IEnumerator disapper()
    {
        for(int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.04f);
            sprRenderer.color = new Color(sprRenderer.color.r, sprRenderer.color.g, sprRenderer.color.b, sprRenderer.color.a - 0.05f);
        }
        sprRenderer.sprite = null;
        sprRenderer.color = Color.white;
    }
    public List<HexaBlock> GetAdjacentBlocks(BlockType type)
    {
        bool isOddBlock = (xPos % 2 == 1);
        
        int sameTypeCnt = 0;

        List<HexaBlock> sameTypeBlocks = new List<HexaBlock>();
        Vector2Int[] offsets = new Vector2Int[6];

        if (isOddBlock)
        {
            offsets[0] = new Vector2Int(-1, 0);
            offsets[1] = new Vector2Int(-1, -1);
            offsets[2] = new Vector2Int(1, 0);
            offsets[3] = new Vector2Int(1, -1);
        }
        else
        {
            offsets[0] = new Vector2Int(-1, 1);
            offsets[1] = new Vector2Int(-1, 0);
            offsets[2] = new Vector2Int(1, 1);
            offsets[3] = new Vector2Int(1, 0);
        }
        offsets[4] = new Vector2Int(0, 1);
        offsets[5] = new Vector2Int(0, -1);


        for (int i = 0; i < 6; i++)
        {
            HexaBlock b = LevelGenerator.Instance.GetBlock(xPos + offsets[i].x, yPos + offsets[i].y);

            if (b && b.blockType == type)
            {
                sameTypeBlocks.Add(b);
                sameTypeCnt++;
            }
        }

        return sameTypeBlocks;
    }

    public void PopObstacle()
    {
        if(blockType == BlockType.OBSTACLE)
        {
            switch(blockStatus)
            {
                case BlockStatus.TOPS:
                    hp = hp - 1;

                    if (hp <= 0)
                    {
                        Matched();
                    }
                    else
                    {
                        sprRenderer.sprite = topsSprite[hp - 1];
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
