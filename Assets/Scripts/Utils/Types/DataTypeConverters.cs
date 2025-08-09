using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Utils.Types
{
    /// <summary>
    /// Globally used data type converters and converter makers.
    /// </summary>
    public class DataTypeConverters
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void RegisterConverters()
        {
            // Boolean -> StyleEnum<DisplayStyle>
            ConverterGroups.RegisterGlobalConverter<bool, StyleEnum<DisplayStyle>>(
                (ref bool isVisible) => isVisible ? DisplayStyle.Flex : DisplayStyle.None
            );

            // Boolean -> PickingMode
            ConverterGroups.RegisterGlobalConverter(
                (ref bool isVisible) => isVisible ? PickingMode.Position : PickingMode.Ignore
            );
        }

        /// <summary>
        /// Create a unidirectional converter group for a specific type.
        /// </summary>
        /// <param name="groupName">Name of the converter group.</param>
        /// <param name="converter">Converter to define.</param>
        /// <typeparam name="TSource">Source data type.</typeparam>
        /// <typeparam name="TDestination">Type to be converted to.</typeparam>
        public static void RegisterUnidirectionalConverterGroup<TSource, TDestination>(
            string groupName,
            TypeConverter<TSource, TDestination> converter
        )
        {
            var group = new ConverterGroup(groupName);
            group.AddConverter(converter);
            ConverterGroups.RegisterConverterGroup(group);
        }

        /// <summary>
        /// Create a bidirectional converter group for a specific type.
        /// </summary>
        /// <param name="groupName">Name of the converter group.</param>
        /// <param name="forwardConverter">Converter from the original source type to the converted type.</param>
        /// <param name="backwardConverter">Converter from the converted type back to the source type.</param>
        /// <typeparam name="TSource">Source data type.</typeparam>
        /// <typeparam name="TDestination">Type to be converted to.</typeparam>
        public static void RegisterBidirectionalConverterGroup<TSource, TDestination>(
            string groupName,
            TypeConverter<TSource, TDestination> forwardConverter,
            TypeConverter<TDestination, TSource> backwardConverter
        )
        {
            var group = new ConverterGroup(groupName);
            group.AddConverter(forwardConverter);
            group.AddConverter(backwardConverter);
            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}
