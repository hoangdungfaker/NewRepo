using PBL3_DUTLibrary.Models;


namespace PBL3_DUTLibrary.Interfaces
{
    public interface IBorrowRepository
    {
        List<Borrow> GetAllBorrows();
        Borrow GetById(int id);
        bool Update(Borrow borrow);
        bool Save();

        bool Update(ProlongRequest prolongRequest);
        bool DeleteBorrow(Borrow borrow);
        List<Borrow> GetAllNotReceivedBorrow();
        List<Borrow> GetAllReceivedBorrow();
        List<Borrow> GetAllOverdueBorrow();
        List<Borrow> GetAllCompletedBorrow();
        List<Borrow> GetAllUnCompletedBorrow();
        List<ProlongRequest> GetAllProlongRequests();

        List<ProlongRequest> GetAllUnCompletedProlongRequests();
        List<ProlongRequest> GetAllCompletedProlongRequests();

        ProlongRequest GetProlongRequestById(int id);
    }
}
