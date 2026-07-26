namespace GameServer.Tests;

public class CHelperTests
{
    [Test]
    public async Task Get_position_maps_row_and_col()
    {
        var position = CHelper.get_position(2, 3);

        await Assert.That(position).IsEqualTo((short)17);
    }

    [Test]
    public async Task Distance_between_adjacent_cells_is_one()
    {
        var distance = CHelper.get_distance((short)0, (short)1);

        await Assert.That(distance).IsEqualTo((short)1);
    }
}
