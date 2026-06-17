using System.Collections.Generic;

public class ShooterTransferRequest
{
    public List<Shooter> Shooters { get; private set; }

    public bool IsGroup => Shooters != null && Shooters.Count > 1;

    private ShooterTransferRequest()
    {
        Shooters = new List<Shooter>();
    }

    public static ShooterTransferRequest CreateSingle(Shooter shooter)
    {
        ShooterTransferRequest request = new ShooterTransferRequest();

        if (shooter != null)
        {
            request.Shooters.Add(shooter);
        }

        return request;
    }

    public static ShooterTransferRequest CreateGroup(IEnumerable<Shooter> shooters)
    {
        ShooterTransferRequest request = new ShooterTransferRequest();

        foreach (Shooter shooter in shooters)
        {
            if (shooter != null)
            {
                request.Shooters.Add(shooter);
            }
        }

        return request;
    }
}