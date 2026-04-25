$(document).ready(function () {
    var defaultAvatarPath = 'https://mdbcdn.b-cdn.net/img/Photos/new-templates/bootstrap-chat/ava1-bg.webp';

    // Helper function to check if an item is overdue
    function isOverdue(deadline, returnedDate) {
        if (!deadline || returnedDate) {
            return false; // Không có deadline hoặc đã trả => không overdue
        }

        // Parse date string từ API (format có thể khác nhau tùy vào API)
        var deadlineDate;
        try {
            // Improved date parsing logic
            if (typeof deadline === 'string') {
                // Thử phân tích deadline từ các định dạng phổ biến
                if (deadline.includes('-')) {
                    // ISO format (YYYY-MM-DD)
                    deadlineDate = new Date(deadline);
                } else if (deadline.includes('/')) {
                    // US format (MM/DD/YYYY) or other slash format
                    var parts = deadline.split('/');
                    if (parts.length === 3) {
                        // Handle both M/D/YYYY and D/M/YYYY formats
                        // Assume MM/DD/YYYY format by default
                        if (parts[0].length <= 2 && parts[1].length <= 2 && parts[2].length === 4) {
                            deadlineDate = new Date(parts[2], parts[0] - 1, parts[1]);
                        } else {
                            // Fallback to direct parsing
                            deadlineDate = new Date(deadline);
                        }
                    } else {
                        deadlineDate = new Date(deadline);
                    }
                } else if (deadline.includes(',')) {
                    // Format "MMM DD, YYYY" (e.g. "Jan 01, 2023")
                    deadlineDate = new Date(deadline);
                } else {
                    // Nếu không nhận dạng được, thử parse trực tiếp
                    deadlineDate = new Date(deadline);
                }
            } else if (deadline instanceof Date) {
                deadlineDate = deadline;
            } else {
                console.warn("Invalid deadline type:", typeof deadline);
                return false;
            }

            if (isNaN(deadlineDate.getTime())) {
                console.warn("Could not parse deadline date:", deadline);
                return false;
            }

            var today = new Date();
            // Reset time part to compare just the dates
            today.setHours(0, 0, 0, 0);
            deadlineDate.setHours(0, 0, 0, 0);

            return deadlineDate < today;
        } catch (e) {
            console.error("Error parsing date:", e);
            return false;
        }
    }

    // Function to create borrows cell with improved UI
    function createBorrowsCell(totalBorrows, activeBorrows) {
        if (totalBorrows === 0) {
            return '<span class="badge badge-light border px-2">0</span>';
        }

        var html = '<div class="d-inline-flex align-items-center">' +
            '<div class="badge badge-primary rounded mr-2">' + totalBorrows + '</div>';

        if (activeBorrows > 0) {
            html += '<small class="text-nowrap"><span class="badge badge-warning rounded-pill mr-1">' +
                activeBorrows + '</span><span class="text-muted">active</span></small>';
        }

        html += '</div>';

        return html;
    }

    // Apply the new borrow cell format to existing table
    function updateBorrowsCells() {
        // Get all rows in the users table
        $('.data-table-export tr, .data-table tr').each(function () {
            var $row = $(this);
            var $borrowsCell = $row.find('td:nth-child(7)'); // Adjust index if needed

            if ($borrowsCell.length && !$borrowsCell.hasClass('formatted')) {
                // Parse the content of the cell
                var cellContent = $borrowsCell.text().trim();
                var totalMatch = cellContent.match(/(\d+)\s*total/i);
                var activeMatch = cellContent.match(/(\d+)\s*active/i);

                if (totalMatch) {
                    var totalBorrows = parseInt(totalMatch[1]);
                    var activeBorrows = activeMatch ? parseInt(activeMatch[1]) : 0;

                    $borrowsCell.html(createBorrowsCell(totalBorrows, activeBorrows));
                    $borrowsCell.addClass('formatted');
                }
            }
        });
    }

    // Call once on page load
    setTimeout(updateBorrowsCells, 500);

    // Update after any AJAX calls that might refresh the table
    $(document).ajaxComplete(function (event, xhr, settings) {
        setTimeout(updateBorrowsCells, 200);
    });

    // Update function for AJAX responses
    function updateUserBorrows(userId, totalBorrows, activeBorrows) {
        // Find the row with the matching user ID
        var row = $('tr[data-user-id="' + userId + '"]');

        if (row.length) {
            // Update the borrows cell
            var borrowsCell = row.find('td.borrows-cell');
            if (borrowsCell.length) {
                borrowsCell.html(createBorrowsCell(totalBorrows, activeBorrows));
            }
        }
    }

    // Add User button
    $('#add-user-btn').click(function () {
        $('#user-form')[0].reset();
        $('#UserId').val(0);
        $('#user-modal-label').text('Add New User');
        $('#image-preview').attr('src', defaultAvatarPath);
        $('#Password').attr('required', true);
        $('#ConfirmPassword').attr('required', true);
        $('#password-section label .text-danger').show();
        $('#confirm-password-section label .text-danger').show();
        $('#Password').attr('placeholder', '');
        clearAllValidationMessages();
        $('#user-modal').modal('show');
    });

    // Edit User button
    $('body').on('click', '.edit-user', function (e) {
        e.preventDefault();
        var userId = $(this).data('id');
        clearAllValidationMessages();
        $.ajax({
            url: '/AdminUsers/GetUserDetails',
            type: 'GET',
            data: { userId: userId },
            success: function (response) {
                if (response.success) {
                    var user = response.data;
                    $('#user-form')[0].reset();
                    $('#UserId').val(user.userId);
                    $('#Username').val(user.username);
                    $('#RealName').val(user.realName);
                    $('#Email').val(user.email);
                    $('#Sdt').val(user.sdt);
                    $('#Role').val(user.role);
                    $('#Status').val(user.status.toString());
                    $('#image-preview').attr('src', user.existingImage || defaultAvatarPath);
                    $('#ExistingImage').val(user.existingImage);

                    // Password not required for edit, only if changing
                    $('#Password').attr('required', false).val('');
                    $('#ConfirmPassword').attr('required', false).val('');
                    $('#Password').attr('placeholder', 'Leave blank to keep current');
                    $('#password-section label .text-danger').hide();
                    $('#confirm-password-section label .text-danger').hide();

                    $('#user-modal-label').text('Edit User');
                    $('#user-modal').modal('show');
                } else {
                    showErrorMessage(response.message || 'Failed to load user details.');
                }
            },
            error: function () {
                showErrorMessage('An error occurred while fetching user details.');
            }
        });
    });

    // View User button
    $('body').on('click', '.view-user', function (e) {
        e.preventDefault();
        var userId = $(this).data('id');
        var userName = $(this).closest('tr').find('td:eq(1)').text().trim();
        var userRole = $(this).closest('tr').find('td:eq(2)').text().trim();
        var userStatus = $(this).closest('tr').find('td:eq(3)').find('.badge').hasClass('badge-success') ? 'Active' : 'Inactive';
        var userImage = $(this).closest('tr').find('td:eq(0) img').attr('src') || defaultAvatarPath;

        // Show the modal immediately with basic information
        $('#details-modal').modal('show');
        showUserDetailsLoading(userId, userName, userRole, userStatus, userImage);

        // Store the userId on the modal for reference
        $('#details-modal').data('user-id', userId);

        console.log("Fetching details for user ID:", userId);
        var requestStartTime = new Date().getTime();

        // Add timeout and better error handling
        $.ajax({
            url: '/AdminUsers/GetUserDetails',
            type: 'GET',
            data: { userId: userId, pageSize: 5 }, // Request limited data initially
            cache: false, // Prevent caching
            timeout: 10000, // 10 seconds timeout
            success: function (response) {
                var requestTime = new Date().getTime() - requestStartTime;
                console.log("User details loaded in: " + requestTime + "ms");

                if (response.success) {
                    renderFullUserDetails(response, userId);
                } else {
                    showUserDetailsError(response.message || 'Could not load user details.', userId);
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading user details:", status, error);

                var errorMessage = 'An error occurred while fetching user details.';

                if (status === 'timeout') {
                    errorMessage = 'Request timed out. The server is taking too long to respond.';
                } else if (status === 'error') {
                    errorMessage = 'Server error occurred. Please check server logs.';
                } else if (status === 'parsererror') {
                    errorMessage = 'Response parsing error. Server returned invalid data.';
                }

                showUserDetailsError(errorMessage, userId);
            }
        });
    });

    // Helper function to show loading state with basic user info
    function showUserDetailsLoading(userId, userName, userRole, userStatus, userImage) {
        var html = '<div class="row">' +
            // Left column with basic user info that we already have
            '<div class="col-md-4 text-center">' +
            '<div class="card-box p-4">' +
            '<img src="' + userImage + '" alt="User Avatar" class="rounded-circle img-thumbnail shadow" style="width:160px; height:160px; object-fit:cover;">' +
            '<h4 class="mt-3 font-weight-bold">' + userName + '</h4>' +
            '<p class="text-muted">' +
            '<span class="badge badge-pill ' + (userRole === 'Admin' ? 'badge-danger' : 'badge-info') + ' px-3 py-2 my-2">' +
            userRole + '</span>' +
            '</p>' +
            '<div class="mb-3">' +
            (userStatus === 'Active' ?
                '<span class="badge badge-success badge-pill px-3 py-2">Active Account</span>' :
                '<span class="badge badge-danger badge-pill px-3 py-2">Inactive Account</span>') +
            '</div>' +
            '</div>' +
            '</div>' +
            // Right column with loading indicator for tabs
            '<div class="col-md-8">' +
            '<div class="card-box p-0">' +
            '<ul class="nav nav-tabs nav-justified" id="userDetailsTabs" role="tablist">' +
            '<li class="nav-item">' +
            '<a class="nav-link active font-weight-bold" id="profile-tab" data-toggle="tab" href="#profile" role="tab">' +
            '<i class="fa fa-user-circle mr-1"></i> Profile</a>' +
            '</li>' +
            '<li class="nav-item">' +
            '<a class="nav-link font-weight-bold" id="stats-tab" data-toggle="tab" href="#stats" role="tab">' +
            '<i class="fa fa-chart-bar mr-1"></i> Statistics</a>' +
            '</li>' +
            '<li class="nav-item">' +
            '<a class="nav-link font-weight-bold" id="history-tab" data-toggle="tab" href="#history" role="tab">' +
            '<i class="fa fa-history mr-1"></i> History</a>' +
            '</li>' +
            '</ul>' +
            '<div class="tab-content p-4 border border-top-0 rounded-bottom" id="userDetailsTabContent" style="min-height: 300px;">';

        // Default tab content with loading indicator
        html += '<div class="tab-pane fade show active" id="profile" role="tabpanel">' +
            '<div class="text-center py-3">' +
            '<div class="spinner-border text-primary mb-3"></div>' +
            '<p>Loading profile details...</p>' +
            '</div>' +
            '</div>' +
            '<div class="tab-pane fade" id="stats" role="tabpanel">' +
            '<div class="text-center py-3">' +
            '<div class="spinner-border text-primary mb-3"></div>' +
            '<p>Loading statistics...</p>' +
            '</div>' +
            '</div>' +
            '<div class="tab-pane fade" id="history" role="tabpanel">' +
            '<div class="text-center py-3">' +
            '<div class="spinner-border text-primary mb-3"></div>' +
            '<p>Loading history...</p>' +
            '</div>' +
            '</div>';

        html += '</div></div></div></div>';

        $('#details-modal-body').html(html);

        // Initialize tabs
        $('#userDetailsTabs a').on('click', function (e) {
            e.preventDefault();
            $(this).tab('show');
        });

        // Show edit button and set user ID
        $('.edit-from-details').show().data('id', userId);
    }

    // Helper function to show error message
    function showUserDetailsError(errorMessage, userId) {
        // Keep the first part of the modal (user image and basic info) intact
        // Only replace the tab content area with error message
        $('#userDetailsTabContent').html(
            '<div class="text-danger text-center py-4">' +
            '<i class="fa fa-exclamation-triangle fa-3x mb-3"></i>' +
            '<h5 class="mb-3">Error Loading Data</h5>' +
            '<p>' + errorMessage + '</p>' +
            '<button class="btn btn-outline-primary mt-3 reload-details" data-user-id="' + userId + '">' +
            '<i class="fa fa-sync-alt mr-1"></i> Try Again</button>' +
            '</div>'
        );
    }

    // Helper function to render the full user details
    function renderFullUserDetails(response, userId) {
        var user = response.data;
        var borrowStats = response.borrowStats || {};
        var recentBorrows = response.recentBorrows || [];

        // Skip re-rendering the left column since we already have basic info displayed
        // Focus only on updating the tab content

        // Profile tab content
        var profileHtml = '<table class="table table-hover table-bordered">' +
            '<tbody>' +
            '<tr><th class="bg-light" style="width:35%;"><i class="fa fa-id-card text-primary mr-1"></i> User ID</th><td>' + user.userId + '</td></tr>' +
            '<tr><th class="bg-light"><i class="fa fa-user text-primary mr-1"></i> Username</th><td>' + (user.username || 'N/A') + '</td></tr>' +
            '<tr><th class="bg-light"><i class="fa fa-signature text-primary mr-1"></i> Full Name</th><td>' + (user.realName || 'N/A') + '</td></tr>' +
            '<tr><th class="bg-light"><i class="fa fa-envelope text-primary mr-1"></i> Email</th><td>' + (user.email || 'N/A') + '</td></tr>' +
            '<tr><th class="bg-light"><i class="fa fa-phone text-primary mr-1"></i> Phone</th><td>' + (user.sdt ? formatPhoneNumber(user.sdt) : 'Not provided') + '</td></tr>' +
            '</tbody>' +
            '</table>';

        // Stats tab content
        var statsHtml = '';
        if (borrowStats && recentBorrows.length > 0) {
            // Get necessary counts
            var activeborrowsFiltered = recentBorrows.filter(function (borrow) {
                return (borrow.status === "Pending" || borrow.status === "Approved" || borrow.status === "Overdue") &&
                    !borrow.returnedDate &&
                    borrow.status !== "Rejected";
            });

            var overdueCount = recentBorrows.filter(function (borrow) {
                var deadlineDate = new Date(borrow.deadline);
                var today = new Date();
                return !borrow.returnedDate && deadlineDate < today;
            }).length;

            var returnedCount = recentBorrows.filter(function (borrow) {
                return borrow.returnedDate != null;
            }).length;

            var rejectedCount = recentBorrows.filter(function (borrow) {
                return borrow.status === "Rejected";
            }).length;

            var pendingCount = recentBorrows.filter(function (borrow) {
                return borrow.status === "Pending" && !borrow.returnedDate;
            }).length;

            var approvedCount = recentBorrows.filter(function (borrow) {
                var deadlineDate = new Date(borrow.deadline);
                var today = new Date();
                return borrow.status === "Approved" && !borrow.returnedDate && deadlineDate >= today;
            }).length;

            // Calculate percentages
            var totalNonRejected = recentBorrows.filter(function (borrow) {
                return borrow.status !== "Rejected";
            }).length;

            var returnRate = (totalNonRejected > 0) ?
                Math.round((returnedCount / totalNonRejected) * 100) : 0;

            // Get activity date
            var mostRecentBorrow = new Date(Math.max.apply(null,
                recentBorrows.map(function (e) { return new Date(e.borrowDate); })));
            var daysLastActive = Math.floor((new Date() - mostRecentBorrow) / (1000 * 60 * 60 * 24));

            // Improved statistics layout with a cleaner card design
            statsHtml += '<div class="card shadow-sm mb-0 h-100">' +
                '<div class="card-header py-2 px-3 bg-light">' +
                '<h5 class="font-16 mb-0">Borrowing Statistics</h5>' +
                '</div>' +
                '<div class="card-body px-3 py-3">';

            // Four statistics boxes in a compact grid layout
            statsHtml += '<div class="row mb-3">' +
                // Total borrows
                '<div class="col-3">' +
                '<div class="p-2 text-center border rounded bg-light">' +
                '<div class="h5 mb-0 font-weight-bold text-primary">' + recentBorrows.length + '</div>' +
                '<div class="small text-muted">Total</div>' +
                '</div>' +
                '</div>' +
                // Active borrows
                '<div class="col-3">' +
                '<div class="p-2 text-center border rounded bg-light">' +
                '<div class="h5 mb-0 font-weight-bold text-warning">' + activeborrowsFiltered.length + '</div>' +
                '<div class="small text-muted">Active</div>' +
                '</div>' +
                '</div>' +
                // Returned
                '<div class="col-3">' +
                '<div class="p-2 text-center border rounded bg-light">' +
                '<div class="h5 mb-0 font-weight-bold text-success">' + returnedCount + '</div>' +
                '<div class="small text-muted">Returned</div>' +
                '</div>' +
                '</div>' +
                // Overdue
                '<div class="col-3">' +
                '<div class="p-2 text-center border rounded bg-light">' +
                '<div class="h5 mb-0 font-weight-bold text-danger">' + overdueCount + '</div>' +
                '<div class="small text-muted">Overdue</div>' +
                '</div>' +
                '</div>' +
                '</div>';

            // Additional statistics in a more organized layout
            statsHtml += '<div class="row">' +
                '<div class="col-6">' +
                '<div class="p-2 border rounded">' +
                '<div class="d-flex justify-content-between align-items-center mb-1">' +
                '<span class="font-weight-medium small">Return Rate:</span>' +
                '<span class="badge badge-light border">' + returnRate + '%</span>' +
                '</div>' +
                '<div class="progress" style="height: 8px;">' +
                '<div class="progress-bar bg-success" role="progressbar" style="width: ' + returnRate + '%"></div>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-6">' +
                '<div class="p-2 border rounded">' +
                '<div class="d-flex justify-content-between align-items-center mb-1">' +
                '<span class="font-weight-medium small">Last Activity:</span>' +
                '<span class="badge badge-light border">' + daysLastActive + ' days ago</span>' +
                '</div>' +
                '<div class="small text-muted">' +
                formatDate(mostRecentBorrow) +
                '</div>' +
                '</div>' +
                '</div>' +
                '</div>';

            // Close card
            statsHtml += '</div></div>';

        } else {
            statsHtml = '<div class="text-center pt-4 pb-5">' +
                '<i class="fa fa-chart-bar text-muted" style="font-size: 3rem;"></i>' +
                '<p class="mt-2 mb-1 text-muted font-weight-medium">No statistics available</p>' +
                '<p class="text-muted small">User has no borrowing history yet</p>' +
                '</div>';
        }

        // History tab
        var historyHtml = '';
        if (recentBorrows && recentBorrows.length > 0) {
            historyHtml += '<div class="d-flex justify-content-between align-items-center mb-2">' +
                '<small class="text-muted font-weight-medium"><i class="fa fa-history mr-1"></i>Borrowing records: <span class="badge badge-primary badge-pill">' + recentBorrows.length + '</span></small>' +
                '</div>' +
                '<div class="table-responsive rounded border" style="max-height: 375px; overflow-y: auto;">' +
                '<table class="table table-sm table-hover mb-0">' +
                '<thead class="thead-light sticky-top" style="top: 0">' +
                '<tr>' +
                '<th style="width: 48%">Book</th>' +
                '<th style="width: 27%">Dates</th>' +
                '<th style="width: 8%">Status</th>' +
                '<th style="width: 17%">Actions</th>' +
                '</tr>' +
                '</thead>' +
                '<tbody>';

            // Only show first 10 borrows for initial load (for performance)
            var showBorrows = recentBorrows.slice(0, 10);

            showBorrows.forEach(function (borrow) {
                var isOverdueStatus = borrow.isOverdue;
                if (isOverdueStatus === undefined && borrow.deadline) {
                    isOverdueStatus = isOverdue(borrow.deadline, borrow.returnedDate);
                }
                var dueDate = borrow.deadline ? formatDate(borrow.deadline) : '-';
                var displayBookTitle = borrow.bookTitle || 'Unknown Book';
                var fullBookTitle = displayBookTitle;
                if (displayBookTitle.length > 30) {
                    displayBookTitle = displayBookTitle.substring(0, 27) + '...';
                }

                // Default book cover if no image
                var bookCover = borrow.bookImage || '/vendors/images/default-book.png';

                // Thiết lập statusClass dựa trên status và isOverdueStatus
                var statusClass = "";
                switch (borrow.status) {
                    case "Returned": statusClass = "badge-success"; break;
                    case "Approved": statusClass = "badge-primary"; break;
                    case "Pending": statusClass = "badge-warning"; break;
                    case "Overdue": statusClass = "badge-danger"; break;
                    case "Rejected": statusClass = "badge-secondary"; break;
                    default: statusClass = "badge-info";
                }

                // Nếu chưa trả (không có returnedDate) và isOverdueStatus là true, đánh dấu là overdue
                if (isOverdueStatus && !borrow.returnedDate && borrow.status !== "Rejected") {
                    statusClass = "badge-danger"; // Ghi đè statusClass thành badge-danger
                }

                historyHtml += '<tr>' +
                    '<td>' +
                    '<div class="d-flex align-items-center">' +
                    '<img src="' + bookCover + '" class="mr-1 border rounded" width="32" height="40" style="object-fit: cover;" alt="Book Cover">' +
                    '<div class="small font-weight-medium text-primary" title="' + fullBookTitle + '">' + displayBookTitle + '</div>' +
                    '</div>' +
                    '</td>' +
                    '<td>' +
                    '<div class="d-flex flex-column" style="font-size: 11px;">' +
                    '<div><i class="fa fa-calendar-plus mr-1"></i>' + formatDate(borrow.borrowDate) + '</div>' +
                    (borrow.returnedDate ?
                        '<div class="text-success"><i class="fa fa-calendar-check mr-1"></i>' + formatDate(borrow.returnedDate) + '</div>' :
                        '<div class="' + statusClass + '"><i class="fa fa-calendar-alt mr-1"></i>Due: ' + dueDate +
                        '</div>'
                    ) +
                    '</div>' +
                    '</td>' +
                    '<td><span class="' + statusClass + ' badge-pill px-1 py-1" style="font-size: 10px;">' +
                    (isOverdueStatus && !borrow.returnedDate ? "Overdue" : borrow.status) +
                    '</span></td>' +
                    '<td>';

                // More compact action buttons with better styling
                if (borrow.status === "Pending") {
                    historyHtml += '<div class="btn-group btn-group-sm">' +
                        '<button class="btn btn-outline-success py-0 px-1 borrow-action-btn" data-action="approve" data-borrow-id="' + borrow.borrowId + '" title="Approve Request">' +
                        '<i class="fa fa-check"></i>' +
                        '</button>' +
                        '<button class="btn btn-outline-danger py-0 px-1 borrow-action-btn" data-action="reject" data-borrow-id="' + borrow.borrowId + '" title="Reject Request">' +
                        '<i class="fa fa-times"></i>' +
                        '</button>' +
                        '</div>';
                } else if ((borrow.status === "Approved" || borrow.status === "Overdue") && !borrow.returnedDate) {
                    historyHtml += '<button class="btn btn-outline-primary btn-sm py-0 px-2 borrow-action-btn" data-action="return" data-borrow-id="' + borrow.borrowId + '" title="Mark as Returned">' +
                        '<i class="fa fa-undo-alt mr-1"></i>Return' +
                        '</button>';
                } else {
                    historyHtml += '<span class="text-muted">-</span>';
                }

                historyHtml += '</td></tr>';
            });

            historyHtml += '</tbody></table></div>';

            // Add load more button if there are more records
            if (recentBorrows.length > 10) {
                historyHtml += '<div class="text-center mt-2">' +
                    '<button class="btn btn-outline-primary btn-sm py-0 px-3 load-more-history" data-user-id="' + userId + '" data-skip="10">' +
                    '<i class="fa fa-sync mr-1"></i> Load More</button>' +
                    '</div>';
            }
        } else {
            historyHtml = '<div class="text-center py-4 my-2 border rounded bg-light">' +
                '<i class="fa fa-book-open text-muted" style="font-size: 2rem;"></i>' +
                '<p class="mt-2 mb-0 small text-muted">No borrowing history available</p>' +
                '</div>';
        }

        // Update each tab content separately without disrupting the UI
        $('#profile').html(profileHtml);
        $('#stats').html(statsHtml);
        $('#history').html(historyHtml);
    }

    // Add handler for the reload button
    $('body').on('click', '.reload-details', function () {
        var userId = $(this).data('user-id');
        $('.view-user[data-id="' + userId + '"]').click();
    });

    // Add handler for loading more history
    $('body').on('click', '.load-more-history', function () {
        var userId = $(this).data('user-id');
        var skip = $(this).data('skip');
        var $button = $(this);

        $button.html('<i class="fa fa-spinner fa-spin mr-1"></i> Loading...').prop('disabled', true);

        $.ajax({
            url: '/AdminUsers/GetUserBorrowHistory',
            type: 'GET',
            data: { userId: userId, skip: skip, take: 10 },
            success: function (response) {
                if (response.success && response.borrows && response.borrows.length > 0) {
                    var historyHtml = '';

                    response.borrows.forEach(function (borrow) {
                        var isOverdueStatus = borrow.isOverdue;
                        if (isOverdueStatus === undefined && borrow.deadline) {
                            isOverdueStatus = isOverdue(borrow.deadline, borrow.returnedDate);
                        }
                        var dueDate = borrow.deadline ? formatDate(borrow.deadline) : '-';
                        var displayBookTitle = borrow.bookTitle || 'Unknown Book';
                        var fullBookTitle = displayBookTitle;
                        if (displayBookTitle.length > 30) {
                            displayBookTitle = displayBookTitle.substring(0, 27) + '...';
                        }

                        // Default book cover if no image
                        var bookCover = borrow.bookImage || '/vendors/images/default-book.png';

                        // Thiết lập statusClass dựa trên status và isOverdueStatus
                        var statusClass = "";
                        switch (borrow.status) {
                            case "Returned": statusClass = "badge-success"; break;
                            case "Approved": statusClass = "badge-primary"; break;
                            case "Pending": statusClass = "badge-warning"; break;
                            case "Overdue": statusClass = "badge-danger"; break;
                            case "Rejected": statusClass = "badge-secondary"; break;
                            default: statusClass = "badge-info";
                        }

                        // Nếu chưa trả (không có returnedDate) và isOverdueStatus là true, đánh dấu là overdue
                        if (isOverdueStatus && !borrow.returnedDate && borrow.status !== "Rejected") {
                            statusClass = "badge-danger"; // Ghi đè statusClass thành badge-danger
                        }

                        historyHtml += '<tr>' +
                            '<td>' +
                            '<div class="d-flex align-items-center">' +
                            '<img src="' + bookCover + '" class="mr-1 border rounded" width="32" height="40" style="object-fit: cover;" alt="Book Cover">' +
                            '<div class="small font-weight-medium text-primary" title="' + fullBookTitle + '">' + displayBookTitle + '</div>' +
                            '</div>' +
                            '</td>' +
                            '<td>' +
                            '<div class="d-flex flex-column" style="font-size: 11px;">' +
                            '<div><i class="fa fa-calendar-plus mr-1"></i>' + formatDate(borrow.borrowDate) + '</div>' +
                            (borrow.returnedDate ?
                                '<div class="text-success"><i class="fa fa-calendar-check mr-1"></i>' + formatDate(borrow.returnedDate) + '</div>' :
                                '<div class="' + statusClass + '"><i class="fa fa-calendar-alt mr-1"></i>Due: ' + dueDate +
                                '</div>'
                            ) +
                            '</div>' +
                            '</td>' +
                            '<td><span class="' + statusClass + ' badge-pill px-1 py-1" style="font-size: 10px;">' +
                            (isOverdueStatus && !borrow.returnedDate ? "Overdue" : borrow.status) +
                            '</span></td>' +
                            '<td>';

                        // More compact action buttons with better styling
                        if (borrow.status === "Pending") {
                            historyHtml += '<div class="btn-group btn-group-sm">' +
                                '<button class="btn btn-outline-success py-0 px-1 borrow-action-btn" data-action="approve" data-borrow-id="' + borrow.borrowId + '" title="Approve Request">' +
                                '<i class="fa fa-check"></i>' +
                                '</button>' +
                                '<button class="btn btn-outline-danger py-0 px-1 borrow-action-btn" data-action="reject" data-borrow-id="' + borrow.borrowId + '" title="Reject Request">' +
                                '<i class="fa fa-times"></i>' +
                                '</button>' +
                                '</div>';
                        } else if ((borrow.status === "Approved" || borrow.status === "Overdue") && !borrow.returnedDate) {
                            historyHtml += '<button class="btn btn-outline-primary btn-sm py-0 px-2 borrow-action-btn" data-action="return" data-borrow-id="' + borrow.borrowId + '" title="Mark as Returned">' +
                                '<i class="fa fa-undo-alt mr-1"></i>Return' +
                                '</button>';
                        } else {
                            historyHtml += '<span class="text-muted">-</span>';
                        }

                        historyHtml += '</td></tr>';
                    });

                    // Append new rows to the table
                    $('#history table tbody').append(historyHtml);

                    // Update the load more button
                    var newSkip = skip + response.borrows.length;
                    if (response.hasMore) {
                        $button.html('<i class="fa fa-sync mr-1"></i> Load More')
                            .prop('disabled', false)
                            .data('skip', newSkip);
                    } else {
                        $button.parent().html('<p class="text-muted small mt-2">All records loaded</p>');
                    }
                } else {
                    $button.parent().html('<p class="text-danger small mt-2">No more records to load</p>');
                }
            },
            error: function () {
                $button.html('<i class="fa fa-sync mr-1"></i> Try Again')
                    .prop('disabled', false);

                // Show error message near the button
                $button.after('<p class="text-danger small mt-2">Failed to load more history</p>');
            }
        });
    });

    // Edit user from details modal
    $('body').on('click', '.edit-from-details', function () {
        var userId = $(this).data('id');
        $('#details-modal').modal('hide');
        $('.edit-user[data-id="' + userId + '"]').click();
    });

    // Helper function to format dates
    function formatDate(dateString) {
        if (!dateString) return '-';
        try {
            const date = new Date(dateString);
            if (isNaN(date.getTime())) return '-'; // Invalid date
            return date.toLocaleDateString('vi-VN', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
            });
        } catch (e) {
            console.error("Error formatting date:", e);
            return '-';
        }
    }

    // Helper function to format phone numbers with leading zeros
    function formatPhoneNumber(phoneStr) {
        if (!phoneStr) return '';
        // Parse to number and format to ensure leading zeros
        let phoneNum = parseInt(phoneStr, 10);
        if (isNaN(phoneNum)) return phoneStr;

        // Format with leading zeros - Vietnamese numbers typically have 10 digits
        // This ensures numbers like "0912345678" display correctly with the leading zero
        return String(phoneNum).padStart(10, '0');
    }

    // Handle file input change for image preview
    $('#UserImage').on('change', function () {
        var reader = new FileReader();
        reader.onload = function (e) {
            $('#image-preview').attr('src', e.target.result);
        }
        if (this.files && this.files[0]) {
            reader.readAsDataURL(this.files[0]);
            $(this).next('.custom-file-label').html(this.files[0].name);
        } else {
            // If no file selected, show existing or default image
            var existingImage = $('#ExistingImage').val();
            $('#image-preview').attr('src', existingImage || defaultAvatarPath);
            $(this).next('.custom-file-label').html('Choose file');
        }
    });

    // Save User (Create or Edit)
    $('#save-user').click(function () {
        clearAllValidationMessages();
        var isFormValid = true;
        var userId = $('#UserId').val();
        var isCreating = (userId == 0 || userId == null || userId == "");

        // Client-side validation
        if (!$('#Username').val().trim()) {
            showValidationMessage($('#Username'), 'Username is required.'); isFormValid = false;
        }
        if (!$('#Email').val().trim()) {
            showValidationMessage($('#Email'), 'Email is required.'); isFormValid = false;
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test($('#Email').val().trim())) {
            showValidationMessage($('#Email'), 'Invalid email format.'); isFormValid = false;
        }

        // Phone number is optional, but must be valid if provided
        var phoneValue = $('#Sdt').val().trim();
        if (phoneValue && !/^0[0-9]{9,10}$/.test(phoneValue)) {
            showValidationMessage($('#Sdt'), 'Phone number must be 10-11 digits starting with 0.'); isFormValid = false;
        }

        if (isCreating || $('#Password').val().trim()) {
            if (!$('#Password').val()) {
                showValidationMessage($('#Password'), 'Password is required.'); isFormValid = false;
            } else if ($('#Password').val().length < 6) {
                showValidationMessage($('#Password'), 'Password must be at least 6 characters.'); isFormValid = false;
            }
            if ($('#Password').val() !== $('#ConfirmPassword').val()) {
                showValidationMessage($('#ConfirmPassword'), 'Passwords do not match.'); isFormValid = false;
            }
        }
        if (!$('#Role').val()) {
            showValidationMessage($('#Role'), 'Role is required.'); isFormValid = false;
        }

        if (!isFormValid) {
            showErrorMessage("Please correct the errors in the form.");
            return;
        }

        var formData = new FormData(document.getElementById('user-form'));
        if (!isCreating && $('#Password').val()) {
            formData.set('NewPassword', $('#Password').val());
            formData.set('ConfirmNewPassword', $('#ConfirmPassword').val());
        }
        if (!isCreating) {
            formData.delete('Password');
            formData.delete('ConfirmPassword');
        }

        var url = isCreating ? '/AdminUsers/Create' : '/AdminUsers/Edit';
        var $saveButton = $(this);
        $saveButton.html('<i class="fa fa-spinner fa-spin"></i> Saving...').prop('disabled', true);

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    $('#user-modal').modal('hide');
                    showSuccessMessage(response.message || (isCreating ? 'User created successfully!' : 'User updated successfully!'));
                    setTimeout(function () { location.reload(); }, 1500);
                } else {
                    if (response.errors) {
                        // Handle validation errors
                        try {
                            // Process each error field
                            Object.keys(response.errors).forEach(function (key) {
                                // Get the field element
                                var fieldId = key.charAt(0).toUpperCase() + key.slice(1);
                                var field = document.getElementById(fieldId);

                                // Handle special case for UserImage
                                if (!field && key.toLowerCase() === "userimage") {
                                    field = document.getElementById('UserImage');
                                }

                                if (field) {
                                    // Get error message text - defensive approach to avoid array notation
                                    var messages = response.errors[key];
                                    var message = "Invalid value";

                                    if (Array.isArray(messages) && messages.length > 0) {
                                        message = messages[0];
                                    } else if (typeof messages === "string") {
                                        message = messages;
                                    }

                                    // Show validation error
                                    showValidationMessage($(field), message);
                                }
                            });
                        } catch (ex) {
                            console.error("Error processing validation errors:", ex);
                        }

                        showErrorMessage(response.message || 'Validation failed. Please check the form.');
                    } else {
                        showErrorMessage(response.message || 'An error occurred.');
                    }
                }
            },
            error: function (xhr) {
                var errorMsg = 'An error occurred while saving the user.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                }
                showErrorMessage(errorMsg);
            },
            complete: function () {
                $saveButton.html('Save').prop('disabled', false);
            }
        });
    });

    // Delete User button
    var userIdToDelete;
    $('body').on('click', '.delete-user', function (e) {
        e.preventDefault();
        userIdToDelete = $(this).data('id');
        $('#delete-modal').modal('show');
    });

    // Confirm Delete
    $('#confirm-delete').click(function () {
        $.ajax({
            url: '/AdminUsers/Delete',
            type: 'POST',
            data: { userId: userIdToDelete },
            success: function (response) {
                $('#delete-modal').modal('hide');
                if (response.success) {
                    showSuccessMessage(response.message || 'User deleted successfully!');
                    setTimeout(function () { location.reload(); }, 1500);
                } else {
                    showErrorMessage(response.message || 'Failed to delete user.');
                }
            },
            error: function () {
                showErrorMessage('An error occurred while deleting the user.');
            }
        });
    });

    // Validation helper functions
    function showValidationMessage($element, message) {
        // Clear any existing validation message
        clearValidationMessage($element);

        // Create error message container with jQuery
        var $errorDiv = $('<div class="validation-error-message text-danger font-12 mt-1"></div>').text(message);

        // Insert after the element or its container for file inputs
        if ($element.hasClass('custom-file-input')) {
            $element.closest('.custom-file').after($errorDiv);
        } else {
            $element.after($errorDiv);
        }

        // Add invalid class
        $element.addClass('is-invalid');
    }

    function clearValidationMessage($element) {
        // Clear error message for file inputs
        if ($element.hasClass('custom-file-input')) {
            var $nextEl = $element.closest('.custom-file').next('.validation-error-message');
            if ($nextEl.length > 0) {
                $nextEl.remove();
            }
        } else {
            // Clear error message for regular inputs
            var $nextEl = $element.next('.validation-error-message');
            if ($nextEl.length > 0) {
                $nextEl.remove();
            }
        }

        // Remove invalid class
        $element.removeClass('is-invalid');
    }

    function clearAllValidationMessages() {
        $('.validation-error-message').remove();
        $('.is-invalid').removeClass('is-invalid');
    }

    function showSuccessMessage(message) {
        Swal.fire({
            icon: 'success',
            title: 'Success!',
            text: message,
            timer: 2000,
            showConfirmButton: false
        });
    }

    function showErrorMessage(message) {
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: message
        });
    }

    // Handle borrow actions (approve, reject, mark returned)
    $('#details-modal-body').on('click', '.borrow-action-btn', function (e) {
        e.preventDefault();
        var action = $(this).data('action');
        var borrowId = $(this).data('borrow-id');
        var $button = $(this);

        // Special handling for return action - show the modal instead
        if (action === 'return') {
            // Get relevant information from the row
            var $row = $button.closest('tr');
            var bookTitle = $row.find('td:eq(0)').text().trim();
            var borrowerName = $('#details-modal').find('.user-name').text().trim();
            var borrowDate = $row.find('td:eq(1)').text().trim();
            var dueDate = $row.find('td:eq(2)').text().trim();
            var currentFee = 0; // Default value can be updated if fee is shown in the table

            // Show the return book modal
            showReturnBookModal(borrowId, bookTitle, borrowerName, borrowDate, dueDate, currentFee);

            return; // Exit early, don't show the confirmation dialog
        }

        // Confirm before taking action for other actions (approve/reject)
        var actionText = '';
        switch (action) {
            case 'approve': actionText = 'approve this borrow request'; break;
            case 'reject': actionText = 'reject this borrow request'; break;
            default: actionText = 'perform this action';
        }

        Swal.fire({
            title: 'Confirm Action',
            text: 'Are you sure you want to ' + actionText + '?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes, proceed',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#3085d6',
        }).then((result) => {
            if (result.isConfirmed) {
                // Show loading state
                $button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Processing...');

                // Send action to server
                $.ajax({
                    url: '/AdminUsers/BorrowAction',
                    type: 'POST',
                    data: { borrowId: borrowId, action: action },
                    success: function (response) {
                        if (response.success) {
                            showSuccessMessage(response.message);

                            // Reload the entire page after short delay to show the success message
                            setTimeout(function () {
                                location.reload();
                            }, 1500);
                        } else {
                            showErrorMessage(response.message || 'Action failed.');
                            // Restore the button
                            restoreActionButton($button, action);
                        }
                    },
                    error: function () {
                        showErrorMessage('Server error occurred. Please try again.');
                        // Restore the button
                        restoreActionButton($button, action);
                    }
                });
            }
        });
    });

    // Helper function to restore action button to original state
    function restoreActionButton($button, action) {
        var icon = '';
        var text = '';

        switch (action) {
            case 'approve':
                icon = 'check';
                text = 'Approve';
                break;
            case 'reject':
                icon = 'times';
                text = 'Reject';
                break;
            case 'return':
                icon = 'undo';
                text = 'Mark Returned';
                break;
            default:
                icon = 'sync';
                text = 'Process';
        }

        $button.prop('disabled', false).html('<i class="fa fa-' + icon + '"></i> ' + text);
    }

    // Update the return book modal to match AdminBooks style
    function createReturnBookModal() {
        // Check if modal already exists
        if ($('#returnBookModal').length) return;

        // Create modal HTML with style matching AdminBooks components
        const modalHtml = `
        <div class="modal fade bs-example-modal-lg" id="returnBookModal" tabindex="-1" role="dialog" aria-labelledby="returnBookModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h4 class="modal-title" id="returnBookModalLabel">Return Book</h4>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-hidden="true">&times;</button>
                    </div>
                    <div class="modal-body">
                        <form id="returnBookForm">
                            <input type="hidden" id="returnBookId" name="borrowId">
                            
                            <div class="form-group">
                                <label class="font-weight-bold">Book Information</label>
                                <p id="returnBookTitle" class="form-control-static font-16 mb-0 text-primary"></p>
                            </div>
                            
                            <div class="form-group">
                                <label class="font-weight-bold">Borrower</label>
                                <p id="returnBookBorrower" class="form-control-static"></p>
                            </div>
                            
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="font-weight-bold">Borrow Date</label>
                                        <p id="returnBookDate" class="form-control-static"></p>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="font-weight-bold">Due Date</label>
                                        <p id="returnBookDueDate" class="form-control-static"></p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="alert alert-info mb-3">
                                <small>
                                    <i class="fa fa-info-circle mr-1"></i>
                                    <strong>Status Notes:</strong><br>
                                    - When returning a book, status will change to <span class="badge badge-success">Returned</span><br>
                                    - <span class="badge badge-danger">Overdue</span> status requires immediate attention<br>
                                    - <span class="badge badge-secondary">Rejected</span> is used for denied borrow requests
                                </small>
                            </div>
                            
                            <hr>
                            <h5 class="font-16 text-dark mb-3">Return Condition</h5>
                            
                            <div class="form-group custom-control custom-checkbox mb-3">
                                <input type="checkbox" class="custom-control-input" id="returnBookDamaged">
                                <label class="custom-control-label" for="returnBookDamaged">
                                    Book is damaged <span class="text-danger">(+50,000₫ fee)</span>
                                </label>
                            </div>
                            
                            <div class="form-group custom-control custom-checkbox mb-3">
                                <input type="checkbox" class="custom-control-input" id="returnBookLost">
                                <label class="custom-control-label" for="returnBookLost">
                                    Book is lost <span class="text-danger">(+100,000₫ fee)</span>
                                </label>
                                <small class="text-muted form-text">Note: Lost books will be removed from inventory</small>
                            </div>
                            
                            <div class="form-group">
                                <label class="font-weight-bold" for="returnBookFee">Total Fee Amount</label>
                                <div class="input-group">
                                    <input type="number" class="form-control" id="returnBookFee" name="fee" value="0">
                                    <div class="input-group-append">
                                        <span class="input-group-text">₫</span>
                                    </div>
                                </div>
                                <small class="form-text text-muted">Fee is automatically calculated based on selections above</small>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">
                            <i class="fa fa-times mr-1"></i> Cancel
                        </button>
                        <button type="button" class="btn btn-primary" id="confirmReturnBook">
                            <i class="fa fa-check mr-1"></i> Confirm Return
                        </button>
                    </div>
                </div>
            </div>
        </div>`;

        // Append modal to body
        $('body').append(modalHtml);

        // Initialize the damage fee and lost fee amounts
        const damageFee = 50000; // Default damage fee
        const lostFee = 100000;  // Default lost fee

        // Handle checkbox changes to update fee
        $('#returnBookDamaged').on('change', function () {
            let currentFee = parseInt($('#returnBookFee').val()) || 0;
            if (this.checked) {
                currentFee += damageFee;
            } else {
                currentFee -= damageFee;
            }
            $('#returnBookFee').val(Math.max(0, currentFee));
            updateFeeDisplay();
        });

        $('#returnBookLost').on('change', function () {
            let currentFee = parseInt($('#returnBookFee').val()) || 0;
            if (this.checked) {
                currentFee += lostFee;
                // If book is lost, automatically check damaged too
                if (!$('#returnBookDamaged').prop('checked')) {
                    $('#returnBookDamaged').prop('checked', true);
                    currentFee += damageFee;
                }
            } else {
                currentFee -= lostFee;
                // Don't automatically uncheck damaged when lost is unchecked
            }
            $('#returnBookFee').val(Math.max(0, currentFee));
            updateFeeDisplay();
        });

        // Format the fee with thousand separator
        function updateFeeDisplay() {
            const fee = parseInt($('#returnBookFee').val()) || 0;
            const formattedFee = new Intl.NumberFormat('vi-VN').format(fee);
            $('#returnBookFee').val(fee); // Keep the numeric value for calculations
        }

        // Handle confirm return button click
        $('#confirmReturnBook').on('click', function () {
            const $button = $(this);
            $button.html('<i class="fa fa-spinner fa-spin mr-1"></i> Processing...').prop('disabled', true);

            const borrowId = $('#returnBookId').val();
            const isDamaged = $('#returnBookDamaged').prop('checked');
            const isLost = $('#returnBookLost').prop('checked');
            const fee = parseInt($('#returnBookFee').val()) || 0;

            // Call the AJAX to return book
            $.ajax({
                url: '/AdminUsers/BorrowAction',
                type: 'POST',
                data: {
                    borrowId: borrowId,
                    action: 'return',
                    isDamaged: isDamaged,
                    isLost: isLost,
                    fee: fee
                },
                success: function (response) {
                    if (response.success) {
                        // Close the modal
                        $('#returnBookModal').modal('hide');

                        // Show success message
                        Swal.fire({
                            icon: 'success',
                            title: 'Success',
                            text: response.message,
                            timer: 2000,
                            showConfirmButton: false
                        });

                        // Reload the page after a short delay
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        // Show error message
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: response.message || 'Failed to process the return'
                        });
                        $button.html('<i class="fa fa-check mr-1"></i> Confirm Return').prop('disabled', false);
                    }
                },
                error: function () {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: 'An error occurred while processing your request'
                    });
                    $button.html('<i class="fa fa-check mr-1"></i> Confirm Return').prop('disabled', false);
                }
            });
        });
    }

    // Update this function to show the return modal
    function showReturnBookModal(borrowId, bookTitle, borrowerName, borrowDate, dueDate, currentFee) {
        // Create modal if it doesn't exist
        createReturnBookModal();

        // Set values in modal
        $('#returnBookId').val(borrowId);
        $('#returnBookTitle').text(bookTitle);
        $('#returnBookBorrower').text(borrowerName);
        $('#returnBookDate').text(borrowDate);
        $('#returnBookDueDate').text(dueDate);

        // Check if the book is overdue
        var deadlineDate = new Date(dueDate);
        var today = new Date();
        var isOverdue = deadlineDate < today;

        // Calculate default overdue fee if book is overdue
        if (isOverdue) {
            // Calculate days overdue
            var daysOverdue = Math.floor((today - deadlineDate) / (1000 * 60 * 60 * 24));
            // Apply minimum fee of 50,000 VND for overdue books, plus 10,000 VND per day overdue
            var defaultOverdueFee = Math.max(50000, daysOverdue * 10000);
            $('#returnBookFee').val(defaultOverdueFee);

            // Show overdue notice in the modal
            var overdueNotice = `<div class="alert alert-danger mb-3">
                <i class="fa fa-exclamation-circle mr-1"></i>
                <strong>Book is overdue by ${daysOverdue} day(s)!</strong><br>
                A minimum penalty fee of 50,000₫ has been applied.
                Additional daily fee: 10,000₫ per day.
            </div>`;
            $('#returnBookForm .alert-info').after(overdueNotice);
        } else {
            $('#returnBookFee').val(currentFee || 0);
        }

        // Reset checkboxes
        $('#returnBookDamaged').prop('checked', false);
        $('#returnBookLost').prop('checked', false);

        // Remove any existing overdue notice from previous modal opening
        $('.alert-danger', '#returnBookForm').remove();

        // Show modal
        $('#returnBookModal').modal('show');
    }

    // Helper function to update borrow status in the user details modal
    function updateBorrowStatusInModal(borrowId, newStatus, returnDate, fee) {
        // Find the borrow row in the table
        const borrowRow = $(`.borrow-action[data-borrow-id="${borrowId}"]`).closest('tr');

        // Update status text and classes
        borrowRow.find('.borrow-status').html(`<span class="badge ${getBadgeClass(newStatus)}">${newStatus}</span>`);

        // Update returned date if provided
        if (returnDate) {
            borrowRow.find('.return-date').text(new Date(returnDate).toLocaleDateString());
        }

        // Update fee if provided
        if (fee !== undefined) {
            borrowRow.find('.borrow-fee').text(fee.toLocaleString());
        }

        // Remove action buttons if book is now returned or rejected
        if (newStatus === 'Returned' || newStatus === 'Rejected') {
            borrowRow.find('.borrow-action').remove();
        }
    }

    // Helper function to get badge class based on status
    function getBadgeClass(status) {
        switch (status) {
            case 'Returned': return 'badge-success';
            case 'Approved': return 'badge-primary';
            case 'Pending': return 'badge-warning';
            case 'Rejected': return 'badge-secondary';
            case 'Overdue': return 'badge-danger';
            default: return 'badge-secondary';
        }
    }
}); 