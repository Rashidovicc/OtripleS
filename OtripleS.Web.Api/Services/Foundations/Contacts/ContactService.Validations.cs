﻿// ---------------------------------------------------------------
// Copyright (c) Coalition of the Good-Hearted Engineers
// FREE TO USE AS LONG AS SOFTWARE FUNDS ARE DONATED TO THE POOR
// ---------------------------------------------------------------

using System;
using System.Linq;
using OtripleS.Web.Api.Models.Contacts;
using OtripleS.Web.Api.Models.Contacts.Exceptions;

namespace OtripleS.Web.Api.Services.Foundations.Contacts
{
    public partial class ContactService
    {
        private void ValidateContactOnCreate(Contact contact)
        {
            ValidateContactIsNotNull(contact);

            Validate
            (
                (Rule: IsInvalidX(contact.Id), Parameter: nameof(Contact.Id)),
                (Rule: IsInvalidX(contact.Information), Parameter: nameof(Contact.Information)),
                (Rule: IsInvalidX(contact.Notes), Parameter: nameof(Contact.Notes)),
                (Rule: IsInvalidX(contact.CreatedBy), Parameter: nameof(Contact.CreatedBy)),
                (Rule: IsInvalidX(contact.UpdatedBy), Parameter: nameof(Contact.UpdatedBy)),
                (Rule: IsInvalidX(contact.CreatedDate), Parameter: nameof(Contact.CreatedDate)),
                (Rule: IsInvalidX(contact.UpdatedDate), Parameter: nameof(Contact.UpdatedDate))
            );

        }

        private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidContactException = new InvalidContactException();

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContactException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContactException.ThrowIfContainsErrors();
        }

        private static dynamic IsInvalidX(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalidX(string text) => new
        {
            Condition = String.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalidX(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        private static void ValidateContactAuditFields(Contact contact)
        {
            switch (contact)
            {
                case { } when IsInvalid(contact.CreatedBy):
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.CreatedBy),
                        parameterValue: contact.CreatedBy);

                case { } when IsInvalid(contact.CreatedDate):
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.CreatedDate),
                        parameterValue: contact.CreatedDate);

                case { } when IsInvalid(contact.UpdatedBy):
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.UpdatedBy),
                        parameterValue: contact.UpdatedBy);

                case { } when IsInvalid(contact.UpdatedDate):
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.UpdatedDate),
                        parameterValue: contact.UpdatedDate);
            }
        }

        private void ValidateContactAuditFieldsOnCreate(Contact contact)
        {
            switch (contact)
            {
                case { } when contact.UpdatedBy != contact.CreatedBy:
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.UpdatedBy),
                        parameterValue: contact.UpdatedBy);

                case { } when contact.UpdatedDate != contact.CreatedDate:
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.UpdatedDate),
                        parameterValue: contact.UpdatedDate);

                case { } when IsDateNotRecent(contact.CreatedDate):
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.CreatedDate),
                        parameterValue: contact.CreatedDate);
            }
        }

        private static void ValidateContactId(Guid contactId)
        {
            if (IsInvalid(contactId))
            {
                throw new InvalidContactException(
                    parameterName: nameof(Contact.Id),
                    parameterValue: contactId);
            }
        }

        private static void ValidateContactIsNotNull(Contact contact)
        {
            if (contact is null)
            {
                throw new NullContactException();
            }
        }

        private bool IsDateNotRecent(DateTimeOffset dateTime)
        {
            DateTimeOffset now = this.dateTimeBroker.GetCurrentDateTime();
            int oneMinute = 1;
            TimeSpan difference = now.Subtract(dateTime);

            return Math.Abs(difference.TotalMinutes) > oneMinute;
        }

        private static bool IsInvalid(DateTimeOffset inputDate) => inputDate == default;
        private static bool IsInvalid(Guid input) => input == Guid.Empty;

        private void ValidateStorageContacts(IQueryable<Contact> storageContacts)
        {
            if (!storageContacts.Any())
            {
                this.loggingBroker.LogWarning("No contacts found in storage.");
            }
        }

        private static void ValidateStorageContact(Contact storageContact, Guid contactId)
        {
            if (storageContact is null)
            {
                throw new NotFoundContactException(contactId);
            }
        }

        private void ValidateContactOnModify(Contact contact)
        {
            ValidateContactIsNotNull(contact);
            ValidateContactId(contact.Id);
            ValidateContactAuditFields(contact);
            ValidateDatesAreNotSame(contact);
            ValidateUpdatedDateIsRecent(contact);
        }

        private static void ValidateAgainstStorageContactOnModify(Contact inputContact, Contact storageContact)
        {
            switch (inputContact)
            {
                case { } when inputContact.CreatedDate != storageContact.CreatedDate:
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.CreatedDate),
                        parameterValue: inputContact.CreatedDate);

                case { } when inputContact.CreatedBy != storageContact.CreatedBy:
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.CreatedBy),
                        parameterValue: inputContact.CreatedBy);

                case { } when inputContact.UpdatedDate == storageContact.UpdatedDate:
                    throw new InvalidContactException(
                        parameterName: nameof(Contact.UpdatedDate),
                        parameterValue: inputContact.UpdatedDate);
            }
        }

        private static void ValidateDatesAreNotSame(Contact contact)
        {
            if (contact.CreatedDate == contact.UpdatedDate)
            {
                throw new InvalidContactException(
                    parameterName: nameof(Contact.UpdatedDate),
                    parameterValue: contact.UpdatedDate);
            }
        }

        private void ValidateUpdatedDateIsRecent(Contact contact)
        {
            if (IsDateNotRecent(contact.UpdatedDate))
            {
                throw new InvalidContactException(
                    parameterName: nameof(contact.UpdatedDate),
                    parameterValue: contact.UpdatedDate);
            }
        }
    }
}
