namespace GameServer.Tests;

public class CHelperTests
{
    [Test]
    public async Task Distance_between_adjacent_cells_is_one()
    {
        var distance = Helper.GetDistance(0, 1);

        _ = await Assert.That(distance).IsEqualTo((short)1);
    }

    [Test]
    public async Task Get_position_maps_row_and_col()
    {
        var position = Helper.GetPosition(2, 3);

        _ = await Assert.That(position).IsEqualTo((short)17);
    }
}
