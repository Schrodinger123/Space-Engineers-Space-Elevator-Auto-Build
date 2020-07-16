////////////EDIT HERE////////////
string lcdName = "";
string projectorName = "";
string connectorName = "";
string baseInventoryBlockName = "";
string mergeUpName = "";
string mergeBottomName = "";
string pistonMiddleName = "";
string pistonUpName = "";
string pistonBottomName = "";
////////////EDIT HERE////////////






IMyTextPanel lcd;
IMyProjector projector;
IMyShipConnector connectorBlock;
IMyInventory baseInventory;
IMyInventory connectorInventory;
IMyShipMergeBlock mergeBlockUp;
IMyShipMergeBlock mergeBlockBottom;
IMyExtendedPistonBase pistonMiddle;
IMyExtendedPistonBase pistonUp;
IMyExtendedPistonBase pistonBottom;

long lagTick = 0;
float lastPistonPos = 0;

public Program()
{
    lcd = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
    projector = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
    connectorBlock = GridTerminalSystem.GetBlockWithName(connectorName) as IMyShipConnector;
    baseInventory = (GridTerminalSystem.GetBlockWithName(baseInventoryBlockName) as IMyCargoContainer).GetInventory();
    connectorInventory = connectorBlock.GetInventory();
    mergeBlockUp = GridTerminalSystem.GetBlockWithName(mergeUpName) as IMyShipMergeBlock;
    mergeBlockBottom = GridTerminalSystem.GetBlockWithName(mergeBottomName) as IMyShipMergeBlock;
    pistonMiddle = GridTerminalSystem.GetBlockWithName(pistonMiddleName) as IMyExtendedPistonBase;
    pistonUp = GridTerminalSystem.GetBlockWithName(pistonUpName) as IMyExtendedPistonBase;
    pistonBottom = GridTerminalSystem.GetBlockWithName(pistonBottomName) as IMyExtendedPistonBase;
}

public void ShowText(string text)
{
    if (lcd != null)
    {
        lcd.WriteText(text, false);
    }
}

public void Main(string argument, UpdateType updateSource)
{
    if (argument == "start")
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }
    else if (argument == "stop")
    {
        Runtime.UpdateFrequency = UpdateFrequency.None;
    }
    else
    {
        Process();
    }
}

public void Process()
{
    bool isUpMerge = CheckUpMerge();
    bool isBottomMerge = CheckBottomMerge();
    
    if (isUpMerge && !isBottomMerge)
    {
        if (pistonMiddle.CurrentPosition <= 0)
        {
            mergeBlockBottom.Enabled = true;
            TryExtend(pistonBottom);

            ShowText("Merging Bottom.");
        }
        else
        {
            ShowText("Bottom Moving.");

            TryRetract(pistonBottom);
            if (pistonBottom.CurrentPosition <= 1)
            {
                TryRetract(pistonMiddle);
                if (lastPistonPos == pistonMiddle.CurrentPosition)
                {
                    lagTick++;
                    ShowText("Lag Tick: " + lagTick.ToString());
                }
                else
                {
                    lagTick = 0;
                }

                if (lagTick > 10)
                {
                    TryExtend(pistonMiddle);
                }

                lastPistonPos = pistonMiddle.CurrentPosition;
            }

            
        }
        return;
    }

    if (!isUpMerge && isBottomMerge)
    {
        if (pistonMiddle.CurrentPosition >= 10)
        {
            mergeBlockUp.Enabled = true;
            TryExtend(pistonUp);

            ShowText("Merging Up.");
        }
        else
        {
            TryRetract(pistonUp);
            TryExtend(pistonMiddle);

            ShowText("Up Moving.");
        }
        return;
    }

    if (isUpMerge && isBottomMerge)
    {
        if (pistonMiddle.CurrentPosition <= 5)
        {
            ShowText("Tring Unlock Up");
            TryUnlockUp();
            return;
        }

        if (pistonMiddle.CurrentPosition > 5)
        {
            ShowText("Tring Unlock Bottom");
            TryUnlockBottom();
            return;
        }
        return;
    }
}

public void TryExtend(IMyExtendedPistonBase piston)
{
    if (piston.Velocity < 0)
    {
        piston.Extend();
    }
}

public void TryRetract(IMyExtendedPistonBase piston)
{
    if (piston.Velocity > 0)
    {
        piston.Retract();
    }
}

public void TryUnlockBottom()
{
    if (!CheckComplete())
    {
        return;
    }

    mergeBlockBottom.Enabled = false;
}

public void TryUnlockUp()
{
    connectorBlock.Disconnect();
    mergeBlockUp.Enabled = false;
}

public bool CheckComplete()
{
    //Check Projector
    if (projector.RemainingBlocks != 0)
    {
        ShowText("RemainingBlocks: " + projector.RemainingBlocks.ToString());
        return false;
    }

    //Check Connected
    connectorBlock.Connect();
    if (connectorBlock.Status != MyShipConnectorStatus.Connected)
    {
        ShowText("Not Connected!");
        return false;
    }

    //Check Connected To Base
    if (!baseInventory.IsConnectedTo(connectorInventory))
    {
        ShowText("Not Connected To Base!");
        return false;
    }

    connectorBlock.Disconnect();

    return true;
}

public bool CheckUpMerge()
{
    //Check Merge
    if (!IsMerged(mergeBlockUp))
    {
        return false;
    }
    return true;
}

public bool CheckBottomMerge()
{
    //Check Merge
    if (!IsMerged(mergeBlockBottom))
    {
        return false;
    }
    return true;
}

public bool IsMerged(IMyShipMergeBlock mrg1)
{
    //Find direction that block merges to
    Matrix mat;
    mrg1.Orientation.GetMatrix(out mat);
    Vector3I right1 = new Vector3I(mat.Right);

    //Check if there is a block in front of merge face
    IMySlimBlock sb = mrg1.CubeGrid.GetCubeBlock(mrg1.Position + right1);
    if (sb == null) return false;

    //Check if the other block is actually a merge block
    IMyShipMergeBlock mrg2 = sb.FatBlock as IMyShipMergeBlock;
    if (mrg2 == null) return false;

    //Check that other block is correctly oriented
    mrg2.Orientation.GetMatrix(out mat);
    Vector3I right2 = new Vector3I(mat.Right);
    return right2 == -right1;
}