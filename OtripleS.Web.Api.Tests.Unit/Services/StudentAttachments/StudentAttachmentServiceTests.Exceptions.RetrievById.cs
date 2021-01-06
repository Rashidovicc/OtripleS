﻿// ---------------------------------------------------------------
// Copyright (c) Coalition of the Good-Hearted Engineers
// FREE TO USE AS LONG AS SOFTWARE FUNDS ARE DONATED TO THE POOR
// ---------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using OtripleS.Web.Api.Models.StudentAttachments;
using OtripleS.Web.Api.Models.StudentAttachments.Exceptions;
using Xunit;

namespace OtripleS.Web.Api.Tests.Unit.Services.StudentAttachments
{
    public partial class StudentAttachmentServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveWhenSqlExceptionOccursAndLogItAsync()
        {
            // given
            Guid randomAttachmentId = Guid.NewGuid();
            Guid inputAttachmentId = randomAttachmentId;
            Guid randomStudentId = Guid.NewGuid();
            Guid inputStudentId = randomStudentId;
            SqlException sqlException = GetSqlException();

            var expectedStudentAttachmentDependencyException
                = new StudentAttachmentDependencyException(sqlException);

            this.storageBrokerMock.Setup(broker =>
                 broker.SelectStudentAttachmentByIdAsync(inputStudentId, inputAttachmentId))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<StudentAttachment> deleteStudentAttachmentTask =
                this.studentAttachmentService.RetrieveStudentAttachmentByIdAsync
                (inputStudentId, inputAttachmentId);

            // then
            await Assert.ThrowsAsync<StudentAttachmentDependencyException>(() =>
                deleteStudentAttachmentTask.AsTask());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCritical(It.Is(SameExceptionAs(expectedStudentAttachmentDependencyException))),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectStudentAttachmentByIdAsync(inputStudentId, inputAttachmentId),
                    Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveWhenDbExceptionOccursAndLogItAsync()
        {
            // given
            Guid randomAttachmentId = Guid.NewGuid();
            Guid randomStudentId = Guid.NewGuid();
            Guid inputAttachmentId = randomAttachmentId;
            Guid inputStudentId = randomStudentId;
            var databaseUpdateException = new DbUpdateException();

            var expectedStudentAttachmentDependencyException =
                new StudentAttachmentDependencyException(databaseUpdateException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectStudentAttachmentByIdAsync(inputStudentId, inputAttachmentId))
                    .ThrowsAsync(databaseUpdateException);

            // when
            ValueTask<StudentAttachment> deleteAttachmentTask =
                this.studentAttachmentService.RetrieveStudentAttachmentByIdAsync
                (inputStudentId, inputAttachmentId);

            // then
            await Assert.ThrowsAsync<StudentAttachmentDependencyException>(
                () => deleteAttachmentTask.AsTask());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameExceptionAs(expectedStudentAttachmentDependencyException))),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectStudentAttachmentByIdAsync(inputStudentId, inputAttachmentId),
                    Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
