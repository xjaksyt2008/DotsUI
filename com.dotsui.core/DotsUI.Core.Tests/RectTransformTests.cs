﻿using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Transforms;

namespace DotsUI.Core.Tests
{
    [TestFixture]
    public class RectTransformTests : ECSTestsFixture
    {
        private EntityArchetype m_ChildArchetype;
        private EntityArchetype m_RootArchetype;

        public override void Setup()
        {
            base.Setup();

            m_ChildArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<Parent>());
            m_RootArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<WorldSpaceRect>());
        }
        /*
         * Test hierarchy:
         *
         * root
         * |_l1c1        [0]
         * |_l1c2        [1]
         * | |_l2c1      [2]
         * | |_l2c2      [3]
         * | |_l2c3      [4]
         * |   |_l3c1    [5]
         *       |_l4c1  [6]
         * |_l1c3        [7]
         */
        private (Entity, NativeArray<Entity>) CreateInitialHierarchy()
        {
            var root = m_Manager.CreateEntity(m_RootArchetype);
            var childArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<WorldSpaceRect>(),
                ComponentType.ReadWrite<Parent>());
            NativeArray<Entity> children = new NativeArray<Entity>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            m_Manager.CreateEntity(childArchetype, children);
            m_Manager.SetComponentData(children[0], new Parent() { Value = root });
            m_Manager.SetComponentData(children[1], new Parent() { Value = root });
            m_Manager.SetComponentData(children[2], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[3], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[4], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[5], new Parent() { Value = children[4] });
            m_Manager.SetComponentData(children[6], new Parent() { Value = children[5] });
            m_Manager.SetComponentData(children[7], new Parent() { Value = root });
            World.GetOrCreateSystem<ParentSystem>().Update();
            return (root, children);
        }

        [Test]
        public void TestHierarchy()
        {
            var (root, children) = CreateInitialHierarchy();

            Assert.AreEqual(3, m_Manager.GetBuffer<Child>(root).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[0]));
            Assert.AreEqual(3, m_Manager.GetBuffer<Child>(children[1]).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[2]));
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[3]));
            Assert.AreEqual(1, m_Manager.GetBuffer<Child>(children[4]).Length);
            Assert.AreEqual(1, m_Manager.GetBuffer<Child>(children[5]).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[6]));
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[7]));
        }
        [Test]
        public void TestParentAdd()
        {
            var (root, children) = CreateInitialHierarchy();

            var child = m_Manager.CreateEntity(m_ChildArchetype);
            m_Manager.SetComponentData(child, new Parent() { Value = children[7] });
            World.GetOrCreateSystem<ParentSystem>().Update();

            Assert.AreEqual(child, m_Manager.GetBuffer<Child>(children[7])[0].Value);
        }

        [Test]
        public void TestParentRemove()
        {
            var (root, children) = CreateInitialHierarchy();

            m_Manager.RemoveComponent<Parent>(children[5]);
            World.GetOrCreateSystem<ParentSystem>().Update();
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[4]));
        }

        [Test]
        public void TestParentChange()
        {
            var (root, children) = CreateInitialHierarchy();

            m_Manager.SetComponentData(children[6], new Parent() { Value = children[4] });
            World.GetOrCreateSystem<ParentSystem>().Update();
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[5]));
            Assert.AreEqual(2, m_Manager.GetBuffer<Child>(children[4]).Length);
        }

        [Test]
        public void TestNonScaledCanvas()
        {

        }

        [Test]
        public void TestPhysicalScaledCanvas()
        {
        }
    }
}
