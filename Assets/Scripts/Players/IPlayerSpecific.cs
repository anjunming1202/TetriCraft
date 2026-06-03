public interface IPlayerSpecific
{ 
   PlayerID PlayerID { get; set; }
   void SetPlayerID(PlayerID playerID)
    {
        PlayerID = playerID;
    }
}
