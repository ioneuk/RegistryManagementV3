﻿using System.Threading.Tasks;
using RegistryManagementV3.Models;
using RegistryManagementV3.Models.Domain;
using RegistryManagementV3.Services.Notifications;

namespace RegistryManagementV3.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISmsUserNotifier _smsUserNotifier;

        public UserService(IUnitOfWork uow, ISmsUserNotifier smsUserNotifier)
        {
            _uow = uow;
            _smsUserNotifier = smsUserNotifier;
        }

        public ApplicationUser GetById(string id)
        {
            return _uow.UserRepository.GetById(id);
        }

        public Task ApproveUserRegistrationByIdAsync(string id)
        {
            var user = _uow.UserRepository.GetById(id);
            user.AccountStatus = AccountStatus.Approved;
            _uow.Save();

            var notification = new SmsNotificationDto
            {
                Content = "Your registration was successful",
                NotificationType = NotificationType.RmRegistrationApproved,
                PhoneNumbers = new[] {user.PhoneNumber}
            };
            return _smsUserNotifier.NotifyAsync(notification);
        }

        public void SetTwoFactorEnabled(ApplicationUser user, bool isTwoFactorEnabled)
        {
            user.TwoFactorEnabled = isTwoFactorEnabled;
            _uow.Save();
        }
    }
}