using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DIRECTION
{
    LEFT_BOTTOM = 0,
    LEFT_TOP,
    RIGHT_BOTTOM,
    RIGHT_TOP,
    BOTTOM,
    TOP,
}

public class MatchGameManager : MonoBehaviour {

    public GameObject[] selectedObjects; // 선택된 오브젝트들
    public List<HexaBlock> matchedBlockList; // 지워질 오브젝트
    
    public Vector3 mousePos;
    public Camera cam;

    bool nowChecking = false;
	// Use this for initialization
	void Start () {
        selectedObjects = new GameObject[2];

        matchedBlockList = new List<HexaBlock>();

        GenerateLogicCoroutine();
    }
	
	// Update is called once per frame
	void Update () {
        if (!nowChecking && Input.GetMouseButtonDown(0))
        {
            mousePos = Input.mousePosition;
            mousePos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z - 1));

            RaycastHit2D hit = Physics2D.Raycast(mousePos, transform.forward * 20.0f);
            Debug.DrawRay(mousePos, transform.forward * 20, Color.red, 1.0f);
            if (hit)
            {
                selectedObjects[0] = hit.collider.gameObject;
                hit.transform.GetComponent<HexaBlock>();
            }
        }
        else if (!nowChecking && Input.GetMouseButtonUp(0))
        {
            if (selectedObjects[0])
            {
                mousePos = Input.mousePosition;
                mousePos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z - 1));

                RaycastHit2D hit = Physics2D.Raycast(mousePos, transform.forward * 20.0f);
                Debug.DrawRay(mousePos, transform.forward * 20, Color.red, 1.0f);
                if (hit)
                {
                    selectedObjects[1] = hit.collider.gameObject;
                    hit.transform.GetComponent<HexaBlock>();

                    StartCoroutine(CheckLogicCoroutine());
                }
            }
        }
    }

    // 생성 후 자동으로 완성된 블록이 있는지 체크해서 재생성
    void GenerateLogicCoroutine()
    {
        nowChecking = true;
        
        CheckAutoMatched(true);

        List<HexaBlock> emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
        while (emptyList.Count != 0)
        {
            foreach (HexaBlock b in emptyList)
            {
                b.SetBlockStatus(b.blockStatus, b.xPos, b.yPos);
            }

            emptyList.Clear();
            emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
            
            CheckAutoMatched(true);

            emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
        }
        nowChecking = false;
    }
    IEnumerator CheckLogicCoroutine()
    {
        nowChecking = true;

        // 선택된 블록 스왑
        SwapSelected();
        yield return new WaitForSeconds(0.5f);

        // 매치 체크
        CheckMatched();
        yield return new WaitForSeconds(1.3f);
        
        // 매치 완료 후 비어있는 칸 움직여주기
        List<HexaBlock> emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
        while(emptyList.Count != 0)
        {
            bool isMoved = true;
            while (isMoved)
            {
                isMoved = false;
                foreach (HexaBlock b in emptyList)
                {
                    if (LevelGenerator.Instance.MoveEmptyBlock(b))
                    {
                        isMoved = true;
                    }
                }

                foreach (HexaBlock b in emptyList)
                {
                    if (LevelGenerator.Instance.MoveEmptySideBlock(b))
                    {
                        isMoved = true;
                    }
                }
                
                // 스폰 위치에 젬 생성
                LevelGenerator.Instance.SpawnGem();

                emptyList.Clear();
                emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
                
                yield return new WaitForSeconds(0.3f);
            }
            // 움직임 끝난 후 자동으로 완성된 블록이 있는지 체크
            CheckAutoMatched(false);
            yield return new WaitForSeconds(1.3f);

            emptyList = LevelGenerator.Instance.hexaBlocks.FindAll(block => block.blockType == BlockType.EMPTY);
        }
        
        selectedObjects[0] = null;
        selectedObjects[1] = null;

        nowChecking = false;
    }

    void SwapSelected()
    {
        if(Vector3.Distance(selectedObjects[0].transform.position, selectedObjects[1].transform.position) < 1.1f)
        {
            HexaBlock block0 = selectedObjects[0].GetComponent<HexaBlock>();
            HexaBlock block1 = selectedObjects[1].GetComponent<HexaBlock>();

            LevelGenerator.Instance.SwapBlocks(block0, block1);
        }
    }

    void CheckMatched()
    {
        HexaBlock block0 = selectedObjects[0].GetComponent<HexaBlock>();
        HexaBlock block1 = selectedObjects[1].GetComponent<HexaBlock>();

        CheckMatchedBlocks(block0);
        CheckMatchedBlocks(block1);
        
        if (matchedBlockList.Count == 0)
        {
            // 매치 실패시 복구
            SwapSelected();
        }
        else
        {
            List<HexaBlock> obstacleList = new List<HexaBlock>();

            foreach (HexaBlock b in matchedBlockList)
            {
                // 주변 장애물 블록 가져와서 리스트에 넣고 매치 후 한번에 터뜨려주기
                List<HexaBlock> adjacentObstacles = b.GetAdjacentBlocks(BlockType.OBSTACLE);
                foreach (HexaBlock obstacle in adjacentObstacles)
                {
                    if(!obstacleList.Contains(obstacle))
                    {
                        obstacleList.Add(obstacle);
                    }
                }
                b.Matched();
            }

            foreach(HexaBlock obstacle in obstacleList)
            {
                obstacle.PopObstacle();
            }
        }
        matchedBlockList.Clear();
    }
    
    // TODO 보드로 체크하는게 나을듯
    void CheckAutoMatched(bool isGenerate)
    {
        foreach(HexaBlock b in LevelGenerator.Instance.hexaBlocks)
        {
            CheckMatchedBlocks(b);
        }

        if (matchedBlockList.Count != 0)
        {
            List<HexaBlock> obstacleList = new List<HexaBlock>();

            foreach (HexaBlock b in matchedBlockList)
            {
                if(!isGenerate)
                {
                    List<HexaBlock> adjacentObstacles = b.GetAdjacentBlocks(BlockType.OBSTACLE);
                    foreach (HexaBlock obstacle in adjacentObstacles)
                    {
                        if (!obstacleList.Contains(obstacle))
                        {
                            obstacleList.Add(obstacle);
                        }
                    }
                }
                b.Matched(isGenerate);
            }

            if (!isGenerate)
            {
                foreach (HexaBlock obstacle in obstacleList)
                {
                    obstacle.PopObstacle();
                }
            }
        }
        matchedBlockList.Clear();
    }

    void CheckMatchedBlocks(HexaBlock block)
    {
        if (block.blockType != BlockType.OBSTACLE && block.blockType != BlockType.EMPTY)
        {
            // LB to RT
            CheckLine(block, DIRECTION.LEFT_BOTTOM, DIRECTION.RIGHT_TOP);

            // LB to RB
            CheckLine(block, DIRECTION.LEFT_TOP, DIRECTION.RIGHT_BOTTOM);

            // B to T
            CheckLine(block, DIRECTION.BOTTOM, DIRECTION.TOP);

            // 4blocks check
            Check4Block(block);
        }
    }
    
    void Check4Block(HexaBlock block, List<HexaBlock> checkList)
    {
        List<HexaBlock> sameTypeBlocks = block.GetAdjacentBlocks(block.blockType);

        foreach (HexaBlock b in sameTypeBlocks)
        {
            int checkCnt = 0;
            // 체크리스트에 있으면 패스
            if (checkList.Contains(b)) continue;
            
            // 새로운 블록의 주변 블록중에 체크리스트 블록이 2개 이상이면 추가
            foreach(HexaBlock adjacentBlock in b.GetAdjacentBlocks(block.blockType))
            {
                if(checkList.Contains(adjacentBlock))
                {
                    checkCnt++;
                }
            }
            if(checkCnt >= 2)
            {
                checkList.Add(b);
                Check4Block(b, checkList);
            }
        }
    }
    // 자신을 포함해서 이어져있는 같은 타입 블록이 4개 이상이고
    // 모든 블록이 인접한 같은 블록의 수가 2개 이상인 경우
    void Check4Block(HexaBlock block)
    {
        List<HexaBlock> sameTypeBlocks = block.GetAdjacentBlocks(block.blockType);
        foreach(HexaBlock b in sameTypeBlocks)
        {
            List<HexaBlock> checkList = new List<HexaBlock>();
            checkList.Add(block);
            checkList.Add(b);

            Check4Block(b, checkList);
            
            // 체크리스트 안에 4개 이상이 들어가있으면 매칭완료
            if(checkList.Count >= 4)
            {
                if (!matchedBlockList.Contains(block))
                {
                    matchedBlockList.Add(block);
                }
                foreach (HexaBlock checkBlock in checkList)
                {
                    if (!matchedBlockList.Contains(checkBlock))
                    {
                        matchedBlockList.Add(checkBlock);
                    }
                }
            }
        }
    }

    void CheckLine(HexaBlock block, DIRECTION dir1, DIRECTION dir2)
    {
        List<HexaBlock> removeBlockList = new List<HexaBlock>();

        if(CalcSameTypeStreakCount(block, dir1, removeBlockList) + CalcSameTypeStreakCount(block, dir2, removeBlockList) >= 2)
        {
            if (!removeBlockList.Contains(block))
            {
                removeBlockList.Add(block);
            }

            foreach (HexaBlock b in removeBlockList)
            {
                if(!matchedBlockList.Contains(b))
                {
                    matchedBlockList.Add(b);
                }
            }
        }
    }

    int CalcSameTypeStreakCount(HexaBlock block, DIRECTION dir, List<HexaBlock> removeBlockList, int streakCnt = 0)
    {
        bool isOddBlock = (block.xPos % 2 == 1);

        int xPos = block.xPos;
        int yPos = block.yPos;

        BlockType blockType = block.blockType;
        
        switch (dir)
        {
            case DIRECTION.LEFT_BOTTOM:
                if (isOddBlock)
                {
                    xPos += -1;

                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                else
                {
                    xPos += -1;
                    yPos += 1;

                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                break;

            case DIRECTION.LEFT_TOP:
                if (isOddBlock)
                {
                    xPos += -1;
                    yPos += -1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                else
                {
                    xPos += -1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                break;

            case DIRECTION.RIGHT_BOTTOM:
                if (isOddBlock)
                {
                    xPos += 1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                else
                {
                    xPos += 1;
                    yPos += 1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                break;

            case DIRECTION.RIGHT_TOP:
                if (isOddBlock)
                {
                    xPos += 1;
                    yPos += -1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                else
                {
                    xPos += 1;
                    if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                    else return streakCnt;
                }
                break;

            case DIRECTION.BOTTOM:
                yPos += 1;
                if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                else return streakCnt;
                break;

            case DIRECTION.TOP:
                yPos += -1;
                if (IsSameType(xPos, yPos, blockType)) streakCnt++;
                else return streakCnt;
                break;
        }

        removeBlockList.Add(LevelGenerator.Instance.GetBlock(xPos, yPos));
        return CalcSameTypeStreakCount(LevelGenerator.Instance.GetBlock(xPos, yPos), dir, removeBlockList, streakCnt);
    }

    bool IsSameType(int xPos, int yPos, BlockType blockType)
    {
        return LevelGenerator.Instance.GetBlockType(xPos, yPos) == blockType;
    }
}
