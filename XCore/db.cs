/*
 * Copyright 2012 www.xcenter.cn
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using System.ORM;
using System.ORM.Operation;
using System.ORM.Caching;

namespace System {

    /// <summary>
    /// XCenter ORM ����Ҫ�Ĺ��ߣ������˶���ĳ��� CRUD (��ȡ/����/����/ɾ��) ��������Ҫ�������Ƿ��ͷ�����
    /// </summary>
    public class db
    {
        /// <summary>
        /// ֻ�����ݿ����
        /// </summary>
        public class ReadOnly
        {
            /// <summary>
            /// ���� sql ����ѯ������һ�� IDataReader
            /// </summary>
            /// <param name="sql"></param>
            /// <returns>����һ�� IDataReader</returns>
            public static IDataReader RunReader(String sql)
            {
                return DataFactory.GetCommand(sql, DbContext.getReadOnlyConnection()).ExecuteReader(CommandBehavior.CloseConnection);
            }
            /// <summary>
            /// ���� sql ����ѯ�����ص��е�������
            /// </summary>
            /// <param name="sql"></param>
            /// <returns>���ص��е�������</returns>
            public static Object RunScalar(String sql)
            {
                return DataFactory.GetCommand(sql, DbContext.getReadOnlyConnection()).ExecuteScalar();
            }
            /// <summary>
            /// ִ�� sql ���
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="sql"></param>
            public static void RunSql(String sql)
            {
                IDbCommand cmd = DataFactory.GetCommand(sql, DbContext.getReadOnlyConnection());
                cmd.ExecuteNonQuery();
                CacheTime.updateTable(typeof(Data.KeyValueData));
            }

            /// <summary>
            /// ���� sql ����ѯ������һ�� DataTable
            /// </summary>
            /// <param name="sql"></param>
            /// <returns></returns>
            public static DataTable RunTable(String sql)
            {
                DataTable dataTable = new DataTable();
                DataFactory.GetAdapter(sql, DbContext.getReadOnlyConnection()).Fill(dataTable);
                return dataTable;
            }
        }

        /// <summary>
        /// ����һ������������Ĳ�ѯ���ߣ�����ֱ�Ӵ����ݿ��������
        /// </summary>
        public static NoCacheDbFinder nocache
        {
            get { return new NoCacheDbFinder(); }
        }

        /// <summary>
        /// ��ѯ T ���Ͷ������������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> findAll<T>() where T : IEntity {

            ObjectInfo state = new ObjectInfo( typeof( T ) );
            state.includeAll();
            IList objList = ObjectPool.FindAll( typeof( T ) );
            if (objList == null) {
                objList = ObjectDb.FindAll( state );
                ObjectPool.AddAll( typeof( T ), objList );
            }

            return getResults<T>( objList );
        }

        /// <summary>
        /// ���� id ��ѯ����
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T findById<T>( int id ) where T : IEntity {

            if (id < 0) return default( T );

            IEntity objCache = ObjectPool.FindOne( typeof( T ), id );
            if (objCache == null) {
                ObjectInfo state = new ObjectInfo( typeof( T ) );
                objCache = ObjectDb.FindById( id, state );
                ObjectPool.Add( objCache );
            }

            return (T)objCache;
        }

        /// <summary>
        /// ���� DataReader ������
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static T fillTo<T>(System.Data.IDataReader dr)
        {
            Type type = Activator.CreateInstance<T>().GetType();
            T obj = (T)Activator.CreateInstance(type);
            var propertypes = type.GetProperties();
            foreach (var pro in propertypes)
            {
                try
                {
                    object value = dr[pro.Name];
                    pro.SetValue(obj, value, null);
                }
                catch { }
            }
            return obj;
        }

        /// <summary>
        /// ���ݲ�ѯ����������һ����ѯ����һ�����ڲ�������ѯ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <returns>���ز�ѯ����xQuery�����Խ�һ����������ֵ�����õ����</returns>
        public static xQuery<T> find<T>( String condition ) where T : IEntity {

            ObjectInfo state = new ObjectInfo( typeof( T ) );
            Query q = ObjectDb.Find( state, condition );
            return new xQuery<T>( q );
        }

        /// <summary>
        /// ���ݲ�ѯ���������ط�ҳ���ݼ���(Ĭ��ÿҳ����20����¼)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <returns>��ҳ�����б���������ǰҳ���ܼ�¼������ҳ����</returns>
        public static DataPage<T> findPage<T>( String condition ) where T : IEntity {

            return findPage<T>( condition, 20 );
        }
        /// <summary>
        /// �浵ģʽ��ҳ(Ĭ�ϰ��� order by Oid asc ����)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <returns>��ҳ�����б���������ǰҳ���ܼ�¼������ҳ����</returns>
        public static DataPage<T> findPageArchive<T>( String condition ) where T : IEntity {
            return findPageArchive<T>( condition, 20 );
        }

        /// <summary>
        /// �浵ģʽ��ҳ(Ĭ�ϰ��� order by Oid asc ����)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <param name="pageSize">ÿҳ��Ҫ��ʾ��������</param>
        /// <returns>��ҳ�����б���������ǰҳ���ܼ�¼������ҳ����</returns>
        public static DataPage<T> findPageArchive<T>( String condition, int pageSize ) where T : IEntity {

            if (strUtil.IsNullOrEmpty( condition )) condition = "order by Id asc";
            if (condition.ToLower().IndexOf("order") < 0) condition = condition + " order by Id asc";

            DataPage<T> list = findPage<T>( condition, pageSize );

            list.Results.Sort( compareEntity );
            return list;
        }

        private static int compareEntity<T>( T p1, T p2 ) where T : IEntity {
            return p1.Id > p2.Id ? -1 : 1;
        }

        /// <summary>
        /// ���ݲ�ѯ������ÿҳ���������ط�ҳ���ݼ���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <param name="pageSize">ÿҳ��Ҫ��ʾ��������</param>
        /// <returns>��ҳ�����б���������ǰҳ���ܼ�¼������ҳ����</returns>
        public static DataPage<T> findPage<T>( String condition, int pageSize ) where T : IEntity {

            if (pageSize <= 0) pageSize = 20;

            ObjectInfo state = new ObjectInfo( typeof( T ) );
            state.includeAll();
            state.Pager.setSize( pageSize );

            IPageList result = ObjectPool.FindPage(typeof(T), condition, state.Pager);
            if (result == null) {

                IList list = ObjectDb.FindPage( state, condition );
                ObjectPage p = state.Pager;
                ObjectPool.AddPage( typeof( T ), condition, p, list );

                result = new PageList();
                result.Results = list;
                result.PageCount = p.PageCount;
                result.RecordCount = p.RecordCount;
                result.Size = p.getSize();
                result.PageBar = p.PageBar;
                result.Current = p.getCurrent();
            }
            else {
                result.PageBar = new ObjectPage(result.RecordCount, result.Size, result.Current).PageBar;
            }

            return new DataPage<T>(result);
        }

        /// <summary>
        /// ���ݲ�ѯ��������ǰҳ���ÿҳ���������ط�ҳ���ݼ���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">��ѯ����</param>
        /// <param name="current">��ǰҳ�루��0��ʼ��</param>
        /// <param name="pageSize">ÿҳ��Ҫ��ʾ��������</param>
        /// <returns>��ҳ�����б���������ǰҳ���ܼ�¼������ҳ����</returns>
        public static DataPage<T> findPage<T>( String condition, int current, int pageSize ) where T : IEntity {

            ObjectInfo state = new ObjectInfo( typeof( T ) );
            state.includeAll();
            state.Pager.setSize(pageSize);
            current++;
            state.Pager.setCurrent( current );

            IList list = ObjectDb.FindPage( state, condition );
            IPageList result = new PageList();
            result.Results = list;
            result.PageCount = state.Pager.PageCount;
            result.RecordCount = state.Pager.RecordCount;
            result.Size = state.Pager.getSize();
            result.PageBar = state.Pager.PageBar;
            result.Current = state.Pager.getCurrent();

            return new DataPage<T>( result );
        }

        /// <summary>
        /// ���� sql ��䣬���ض����б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static List<T> findBySql<T>( String sql ) where T : IEntity {

            IList objList = ObjectPool.FindBySql( sql, typeof( T ) );
            if (objList == null) {
                objList = ObjectDb.FindBySql( sql, typeof( T ) );
                ObjectPool.AddSqlList( sql, objList );
            }

            return getResults<T>( (IList)objList );

        }

        //public static DataPage<T> findPageBySql<T>( String sql, int pageSize ) where T : IEntity {

        //    if (sql == null) throw new ArgumentNullException();

        //    String mysql = sql.ToLower();

        //    String[] arrItem = strUtil.Split( mysql, "where" );

        //    String queryString = arrItem[1];

        //    String[] arrSelect = strUtil.Split( arrItem[0], "from" );
        //    String selectProperty = arrSelect[0];


        //    PageCondition pc = new PageCondition();
        //    pc.ConditionStr = queryString;
        //    pc.Property = selectProperty;

        //    //pc.CurrentPage = state.Pager.getCurrent();
        //    pc.Size = pageSize;

        //    String sql = new SqlBuilder( state.EntityInfo ).GetPageSql( pc );



        //}


        //-------------------------------------------------------------------------

        /// <summary>
        /// ������������ݿ�
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>����һ��������� Result��������������� Result �а���������Ϣ�����û�д���result.Info����obj</returns>
        public static Result insert( Object obj ) {

            if (obj == null) throw new ArgumentNullException();

            Result result = ObjectDb.Insert( (IEntity)obj );
            return result;
        }

        /// <summary>
        /// ���¶��󣬲��������ݿ�
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>����һ��������� Result��������������� Result �а���������Ϣ</returns>
        public static Result update( Object obj ) {

            if (obj == null) throw new ArgumentNullException();

            Result result = ObjectDb.Update( (IEntity)obj );
            ObjectPool.Update( (IEntity)obj );
            return result;
        }

        /// <summary>
        /// ��������Զ��ж����������
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>����һ��������� Result��������������� Result �а���������Ϣ</returns>
        public static Result Save(Object obj)
        {

            if (obj == null) throw new ArgumentNullException();
            if (((IEntity)obj).Id > 0)
            {
                return update(obj);
            }
            else
            {
                return insert(obj);
            }
        }

        /// <summary>
        /// ֻ�޸Ķ����ĳ���ض�����
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName">��Ҫ�޸ĵ���������</param>
        public static void update( Object obj, String propertyName ) {

            if (obj == null) throw new ArgumentNullException();

            ObjectDb.Update( (IEntity)obj, propertyName );
            ObjectPool.Update( (IEntity)obj );
        }

        /// <summary>
        /// ֻ�޸Ķ�����ض�����
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="arrPropertyName">��Ҫ�޸ĵ����Ե�����</param>
        public static void update( Object obj, String[] arrPropertyName ) {

            if (obj == null) throw new ArgumentNullException();

            ObjectDb.Update( (IEntity)obj, arrPropertyName );
            ObjectPool.Update( (IEntity)obj );
        }

        /// <summary>
        /// ���������������¶���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">���µĲ���</param>
        /// <param name="condition">���µ�����</param>
        public static void updateBatch<T>( String action, String condition ) where T : IEntity {
            IEntity obj = Entity.New( typeof( T ).FullName );
            ObjectDb.UpdateBatch( obj, action, condition );
        }

        /// <summary>
        /// ɾ������
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>������Ӱ�������</returns>
        public static int delete( Object obj ) {

            if (obj == null) throw new ArgumentNullException();

            int num = ObjectDb.Delete( (IEntity)obj );
            ObjectPool.Delete( (IEntity)obj );
            return num;
        }

        /// <summary>
        /// ���� id ɾ������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">����� id</param>
        /// <returns>������Ӱ�������</returns>
        public static int delete<T>( int id ) where T : IEntity {
            int num = ObjectDb.Delete( typeof( T ), id );
            ObjectPool.Delete( typeof( T ), id );
            return num;
        }

        /// <summary>
        /// ������������ɾ������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">ɾ������</param>
        /// <returns>������Ӱ�������</returns>
        public static int deleteBatch<T>( String condition ) where T : IEntity {
            return ObjectDb.DeleteBatch( typeof( T ), condition );
        }

        //-------------------------------------------------------------------------

        /// <summary>
        /// ͳ�ƶ����������Ŀ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>��������</returns>
        public static int count<T>() where T : IEntity {

            int countResult = ObjectPool.FindCount( typeof( T ) );
            if (countResult == -1) {
                countResult = ObjectDb.Count( typeof( T ) );
                ObjectPool.AddCount( typeof( T ), countResult );
            }

            return countResult;
        }

        /// <summary>
        /// ��������ͳ�ƶ����������Ŀ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition">ͳ������</param>
        /// <returns>��������</returns>
        public static int count<T>( String condition ) where T : IEntity {

            int countResult = ObjectPool.FindCount( typeof( T ), condition );
            if (countResult == -1) {
                countResult = ObjectDb.Count( typeof( T ), condition );
                ObjectPool.AddCount( typeof( T ), condition, countResult );
            }

            return countResult;
        }

        //-------------------------------------------------------------------------

        internal static List<T> getResults<T>( IList list ) {
            List<T> results = new List<T>();
            foreach (T obj in list) {
                results.Add( obj );
            }
            return results;
        }

        //-------------------------------------------------------------------------

        /// <summary>
        /// ���� sql ����ѯ������һ�� IDataReader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns>����һ�� IDataReader</returns>
        public static IDataReader RunReader<T>( String sql ) {
            return DataFactory.GetCommand( sql, DbContext.getConnection( typeof( T ) ) ).ExecuteReader( CommandBehavior.CloseConnection );
        }
        /// <summary>
        /// ���� sql ����ѯ������һ�� IDataReader
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>����һ�� IDataReader</returns>
        public static IDataReader RunReader( String sql ) {
            return DataFactory.GetCommand(sql, DbContext.getConnection(typeof(Data.KeyValueData))).ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// ���� sql ����ѯ�����ص��е�������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns>���ص��е�������</returns>
        public static Object RunScalar<T>( String sql ) {
            return DataFactory.GetCommand( sql, DbContext.getConnection( typeof( T ) ) ).ExecuteScalar();
        }
        /// <summary>
        /// ���� sql ����ѯ�����ص��е�������
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>���ص��е�������</returns>
        public static Object RunScalar( String sql ) {
            return DataFactory.GetCommand(sql, DbContext.getConnection(typeof(Data.KeyValueData))).ExecuteScalar();
        }

        /// <summary>
        /// ִ�� sql ���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        public static void RunSql<T>( String sql ) {
            IDbCommand cmd = DataFactory.GetCommand( sql, DbContext.getConnection( typeof( T ) ) );
            cmd.ExecuteNonQuery();
            CacheTime.updateTable( typeof( T ) );
        }
        /// <summary>
        /// ִ�� sql ���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        public static void RunSql(String sql)
        {
            IDbCommand cmd = DataFactory.GetCommand(sql, DbContext.getConnection(typeof(Data.KeyValueData)));
            cmd.ExecuteNonQuery();
            CacheTime.updateTable(typeof(Data.KeyValueData));
        }

        /// <summary>
        /// ���� sql ����ѯ������һ�� DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataTable RunTable<T>( String sql ) {
            DataTable dataTable = new DataTable();
            DataFactory.GetAdapter( sql, DbContext.getConnection( typeof( T ) ) ).Fill( dataTable );
            return dataTable;
        }

        /// <summary>
        /// ���� sql ����ѯ������һ�� DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataTable RunTable( String sql ) {
            DataTable dataTable = new DataTable();
            DataFactory.GetAdapter(sql, DbContext.getConnection(typeof(Data.KeyValueData))).Fill(dataTable);
            return dataTable;
        }

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public static void BeginTransaction()
        {
            System.Data.DbContext.beginAndMarkTransactionAll();
        }
        /// <summary>
        /// �ύ���񲢹ر�����
        /// </summary>
        public static void CommitTransaction()
        {
            System.Data.DbContext.commitAll();
        }
        /// <summary>
        /// �ع����񲢹ر�����
        /// </summary>
        public static void RollBackTransaction()
        {
            System.Data.DbContext.rollbackAll();
        }


    }
}