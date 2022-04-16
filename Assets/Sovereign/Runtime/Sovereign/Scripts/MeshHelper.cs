namespace NoFS.DayLight.Sovereign {
   public class MeshHelper {
      public MeshHelper(SvrnMaster master, SvrnMaster.Channel channel) {
         manager = master.getMeshManagerInstance(channel);
      }

      public readonly MeshManager manager;
   }
}