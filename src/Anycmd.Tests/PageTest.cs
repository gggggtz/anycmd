﻿
namespace Anycmd.Tests
{
    using Ac.ViewModels.Infra.FunctionViewModels;
    using Ac.ViewModels.Infra.UIViewViewModels;
    using Engine.Ac;
    using Engine.Ac.Messages.Infra;
    using Engine.Host.Ac.Infra;
    using Moq;
    using Repositories;
    using System;
    using System.Linq;
    using Xunit;

    public class PageTest
    {
        #region PageSet
        [Fact]
        public void PageSet()
        {
            var host = TestHelper.GetAcDomain();
            Assert.Equal(0, host.UiViewSet.Count());

            var entityId = Guid.NewGuid();

            host.Handle(new AddFunctionCommand(new FunctionCreateInput
            {
                Id = entityId,
                Code = "fun1",
                Description = string.Empty,
                DeveloperId = host.SysUsers.GetDevAccounts().First().Id,
                IsEnabled = 1,
                IsManaged = true,
                ResourceTypeId = host.ResourceTypeSet.First().Id,
                SortCode = 10
            }));
            FunctionState functionById;
            Assert.Equal(1, host.FunctionSet.Count());
            Assert.True(host.FunctionSet.TryGetFunction(entityId, out functionById));
            UiViewState pageById;
            host.Handle(new AddUiViewCommand(new UiViewCreateInput
            {
                Id = entityId,
                Icon = null,
                Tooltip = null
            }));
            Assert.Equal(1, host.UiViewSet.Count());
            Assert.True(host.UiViewSet.TryGetUiView(entityId, out pageById));
            bool catched = false;
            try
            {
                host.Handle(new AddUiViewCommand(new UiViewCreateInput
                {
                    Id = Guid.NewGuid(),
                    Icon = null,
                    Tooltip = null
                }));
            }
            catch (Exception)
            {
                catched = true;
            }
            finally
            {
                Assert.True(catched);
            }
            host.Handle(new UpdateUiViewCommand(new UiViewUpdateInput
            {
                Id = entityId,
                Icon = null,
                Tooltip = null
            }));
            Assert.Equal(1, host.UiViewSet.Count());
            Assert.True(host.UiViewSet.TryGetUiView(entityId, out pageById));

            host.Handle(new RemoveUiViewCommand(entityId));
            Assert.False(host.UiViewSet.TryGetUiView(entityId, out pageById));
            Assert.Equal(0, host.UiViewSet.Count());
        }
        #endregion

        #region PageSetShouldRollbackedWhenPersistFailed
        [Fact]
        public void PageSetShouldRollbackedWhenPersistFailed()
        {
            var host = TestHelper.GetAcDomain();
            Assert.Equal(0, host.UiViewSet.Count());

            host.RemoveService(typeof(IRepository<UiView>));
            var moPageRepository = host.GetMoqRepository<UiView, IRepository<UiView>>();
            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();
            moPageRepository.Setup(a => a.Add(It.Is<UiView>(b => b.Id == entityId1))).Throws(new DbException(entityId1.ToString()));
            moPageRepository.Setup(a => a.Update(It.Is<UiView>(b => b.Id == entityId2))).Throws(new DbException(entityId2.ToString()));
            moPageRepository.Setup(a => a.Remove(It.Is<UiView>(b => b.Id == entityId2))).Throws(new DbException(entityId2.ToString()));
            moPageRepository.Setup<UiView>(a => a.GetByKey(entityId1)).Returns(new UiView { Id = entityId1 });
            moPageRepository.Setup<UiView>(a => a.GetByKey(entityId2)).Returns(new UiView { Id = entityId2 });
            host.AddService(typeof(IRepository<UiView>), moPageRepository.Object);

            host.Handle(new AddFunctionCommand(new FunctionCreateInput
            {
                Id = entityId1,
                Code = "fun1",
                Description = string.Empty,
                DeveloperId = host.SysUsers.GetDevAccounts().First().Id,
                IsEnabled = 1,
                IsManaged = true,
                ResourceTypeId = host.ResourceTypeSet.First().Id,
                SortCode = 10
            }));
            host.Handle(new AddFunctionCommand(new FunctionCreateInput
            {
                Id = entityId2,
                Code = "fun2",
                Description = string.Empty,
                DeveloperId = host.SysUsers.GetDevAccounts().First().Id,
                IsEnabled = 1,
                IsManaged = true,
                ResourceTypeId = host.ResourceTypeSet.First().Id,
                SortCode = 10
            }));
            FunctionState functionById;
            Assert.Equal(2, host.FunctionSet.Count());
            Assert.True(host.FunctionSet.TryGetFunction(entityId1, out functionById));
            Assert.True(host.FunctionSet.TryGetFunction(entityId2, out functionById));

            bool catched = false;
            try
            {
                host.Handle(new AddUiViewCommand(new UiViewCreateInput
                {
                    Id = entityId1
                }));
            }
            catch (Exception e)
            {
                Assert.Equal(e.GetType(), typeof(DbException));
                catched = true;
                Assert.Equal(entityId1.ToString(), e.Message);
            }
            finally
            {
                Assert.True(catched);
                Assert.Equal(0, host.UiViewSet.Count());
            }

            host.Handle(new AddUiViewCommand(new UiViewCreateInput
            {
                Id = entityId2
            }));
            Assert.Equal(1, host.UiViewSet.Count());

            catched = false;
            try
            {
                host.Handle(new UpdateUiViewCommand(new UiViewUpdateInput
                {
                    Id = entityId2
                }));
            }
            catch (Exception e)
            {
                Assert.Equal(e.GetType(), typeof(DbException));
                catched = true;
                Assert.Equal(entityId2.ToString(), e.Message);
            }
            finally
            {
                Assert.True(catched);
                Assert.Equal(1, host.UiViewSet.Count());
                UiViewState page;
                Assert.True(host.UiViewSet.TryGetUiView(entityId2, out page));
            }

            catched = false;
            try
            {
                host.Handle(new RemoveUiViewCommand(entityId2));
            }
            catch (Exception e)
            {
                Assert.Equal(e.GetType(), typeof(DbException));
                catched = true;
                Assert.Equal(entityId2.ToString(), e.Message);
            }
            finally
            {
                Assert.True(catched);
                UiViewState page;
                Assert.True(host.UiViewSet.TryGetUiView(entityId2, out page));
                Assert.Equal(1, host.UiViewSet.Count());
            }
        }
        #endregion
    }
}