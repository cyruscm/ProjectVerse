﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Barebones.Networking;

namespace Barebones.MasterServer {
    public class ObservableDictionary : ObservableBase {
        private const int SetOperation = 0;
        private const int RemoveOperation = 1;

        private readonly Queue<UpdateEntry> _updates;

        public ObservableDictionary(short key) : this(key, null) {
        }

        public ObservableDictionary(short key, Dictionary<string, string> defaultValues) : base(key) {
            _updates = new Queue<UpdateEntry>();

            UnderlyingDictionary = defaultValues == null
                ? new Dictionary<string, string>()
                : defaultValues.ToDictionary(k => k.Key, k => k.Value);
        }

        /// <summary>
        ///     Returns an immutable list of values
        /// </summary>
        public IEnumerable<string> Values => UnderlyingDictionary.Values;

        /// <summary>
        ///     Returns an immutable list of key-value pairs
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Pairs => UnderlyingDictionary.ToList();

        /// <summary>
        ///     Returns a mutable dictionary
        /// </summary>
        public Dictionary<string, string> UnderlyingDictionary { get; }

        public void SetValue(string key, string value) {
            if (value == null) {
                Remove(key);
                return;
            }

            if (UnderlyingDictionary.ContainsKey(key))
                UnderlyingDictionary[key] = value;
            else
                UnderlyingDictionary.Add(key, value);

            MarkDirty();
            _updates.Enqueue(new UpdateEntry {
                Key = key,
                Operation = SetOperation,
                Value = value
            });
        }

        public void Remove(string key) {
            UnderlyingDictionary.Remove(key);

            MarkDirty();
            _updates.Enqueue(new UpdateEntry {
                Key = key,
                Operation = RemoveOperation
            });
        }

        public string GetValue(string key) {
            string result;
            UnderlyingDictionary.TryGetValue(key, out result);
            return result;
        }

        public override byte[] ToBytes() {
            return UnderlyingDictionary.ToBytes();
        }

        public override void FromBytes(byte[] data) {
            UnderlyingDictionary.FromBytes(data);
        }

        public override string SerializeToString() {
            var obj = new JSONObject();

            foreach (var pair in UnderlyingDictionary) obj.AddField(pair.Key, pair.Value);

            return obj.ToString();
        }

        public override void DeserializeFromString(string value) {
            var parsed = new JSONObject(value);
            var keys = parsed.keys;
            var list = parsed.list;

            if (keys == null)
                return;

            for (var i = 0; i < keys.Count; i++) UnderlyingDictionary[keys[i]] = list[i].str;
        }

        public override byte[] GetUpdates() {
            using (var ms = new MemoryStream()) {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms)) {
                    writer.Write(_updates.Count);

                    foreach (var update in _updates) {
                        writer.Write(update.Operation);
                        writer.Write(update.Key);

                        if (update.Operation != RemoveOperation) writer.Write(update.Value);
                    }
                }

                return ms.ToArray();
            }
        }

        public override void ApplyUpdates(byte[] data) {
            using (var ms = new MemoryStream(data)) {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms)) {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++) {
                        var operation = reader.ReadByte();
                        var key = reader.ReadString();

                        if (operation == RemoveOperation) {
                            UnderlyingDictionary.Remove(key);
                            continue;
                        }

                        var value = reader.ReadString();

                        if (UnderlyingDictionary.ContainsKey(key))
                            UnderlyingDictionary[key] = value;
                        else
                            UnderlyingDictionary.Add(key, value);
                    }
                }
            }

            MarkDirty();
        }

        public override void ClearUpdates() {
            _updates.Clear();
        }

        private struct UpdateEntry {
            public byte Operation;
            public string Key;
            public string Value;
        }
    }
}