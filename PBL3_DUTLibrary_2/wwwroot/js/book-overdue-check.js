// Function to check if a book is overdue
function isBookOverdue(borrowDate) {
    if (!borrowDate) return false;
    
    try {
        // Default loan period (10 days)
        const LOAN_PERIOD_DAYS = 10;
        const borrowDateObj = new Date(borrowDate);
        
        if (isNaN(borrowDateObj.getTime())) {
            console.warn("Invalid borrow date:", borrowDate);
            return false;
        }
        
        // Calculate due date
        const dueDate = new Date(borrowDateObj);
        dueDate.setDate(dueDate.getDate() + LOAN_PERIOD_DAYS);
        
        // If today is after due date, it's overdue
        return new Date() > dueDate;
    } catch (e) {
        console.error("Error checking overdue status:", e);
        return false;
    }
}

// Function to run after modal content is loaded
function updateOverdueStatus() {
    // Add event handler for when modal is fully shown
    $(document).on('shown.bs.modal', '#details-modal', function() {
        console.log("Details modal shown, checking for overdue items");
        
        // Find all borrow history items in the modal
        $('.list-group-item').each(function() {
            const $item = $(this);
            const $badge = $item.find('.badge');
            
            // Only process items with "Not Returned Yet" status
            if ($badge.text().trim() === 'Not Returned Yet') {
                // Extract the borrow date from the item
                const borrowDateText = $item.find('.ml-1').text();
                const borrowDate = borrowDateText.replace('Borrowed:', '').trim();
                
                // Check if it's overdue
                if (isBookOverdue(borrowDate)) {
                    console.log("Found overdue item:", borrowDate);
                    // Update the badge to show "Overdue" with danger styling
                    $badge.removeClass('badge-warning').addClass('badge-danger');
                    $badge.text('Overdue');
                }
            }
        });
    });
}

// Initialize when document is ready
$(document).ready(function() {
    console.log("Book overdue check script loaded");
    updateOverdueStatus();
});

